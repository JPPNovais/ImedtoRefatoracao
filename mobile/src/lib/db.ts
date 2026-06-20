/* ─────────────────────────────────────────────────────────────
   Persistência LOCAL da aplicação (SQLite) — cache offline leve e
   rascunhos de evolução, conforme §7 do design brief.
   NÃO substitui o backend: é só cache/draft do device. Nenhuma
   regra de negócio vive aqui; a fonte da verdade é a API.

   - Nativo (iOS/Android): @capacitor-community/sqlite.
   - Web (dev): fallback em localStorage com a MESMA API, para o
     app rodar no navegador sem o runtime wasm do jeep-sqlite.

   Segurança (LGPD §6):
   - O campo `texto` do rascunho (conteúdo clínico) é cifrado com
     AES-GCM 256-bit via WebCrypto antes de gravar em SQLite/localStorage.
   - A chave é gerada uma vez e persistida em @capacitor/preferences
     (Keychain no iOS, EncryptedSharedPreferences no Android).
   - No fallback web: o rascunho NÃO é persistido em localStorage
     (rascunho só existe em memória durante a sessão), eliminando PII
     em texto puro no navegador.
   ───────────────────────────────────────────────────────────── */
import { Capacitor } from "@capacitor/core"
import { Preferences } from "@capacitor/preferences"
import {
  CapacitorSQLite,
  SQLiteConnection,
  type SQLiteDBConnection,
} from "@capacitor-community/sqlite"

const DB_NAME = "imedto_local"
const DRAFT_KEY_PREFS = "imedto.draft.key"

/* ── Cifra de rascunho (AES-GCM 256-bit, WebCrypto) ─────────────────────────
   Chave derivada uma vez no primeiro uso e armazenada em @capacitor/preferences
   (Keychain/EncryptedSharedPreferences). IV aleatório de 12 bytes por operação.
   Resultado serializado como "base64(iv):base64(ciphertext)". */

let _draftKey: CryptoKey | null = null

async function getDraftKey(): Promise<CryptoKey> {
  if (_draftKey) return _draftKey

  const { value } = await Preferences.get({ key: DRAFT_KEY_PREFS })
  if (value) {
    // Importa chave salva (raw bytes em base64)
    const raw = Uint8Array.from(atob(value), (c) => c.charCodeAt(0))
    _draftKey = await crypto.subtle.importKey("raw", raw, "AES-GCM", false, ["encrypt", "decrypt"])
  } else {
    // Gera nova chave e persiste
    _draftKey = await crypto.subtle.generateKey({ name: "AES-GCM", length: 256 }, true, ["encrypt", "decrypt"])
    const raw = await crypto.subtle.exportKey("raw", _draftKey)
    await Preferences.set({ key: DRAFT_KEY_PREFS, value: btoa(String.fromCharCode(...new Uint8Array(raw))) })
  }
  return _draftKey
}

async function cifrarTexto(texto: string): Promise<string> {
  const key = await getDraftKey()
  const iv = crypto.getRandomValues(new Uint8Array(12))
  const encoded = new TextEncoder().encode(texto)
  const cipher = await crypto.subtle.encrypt({ name: "AES-GCM", iv }, key, encoded)
  const ivB64 = btoa(String.fromCharCode(...iv))
  const ctB64 = btoa(String.fromCharCode(...new Uint8Array(cipher)))
  return `${ivB64}:${ctB64}`
}

async function decifrarTexto(cifrado: string): Promise<string> {
  const key = await getDraftKey()
  const [ivB64, ctB64] = cifrado.split(":")
  const iv = Uint8Array.from(atob(ivB64), (c) => c.charCodeAt(0))
  const ct = Uint8Array.from(atob(ctB64), (c) => c.charCodeAt(0))
  const plain = await crypto.subtle.decrypt({ name: "AES-GCM", iv }, key, ct)
  return new TextDecoder().decode(plain)
}

export interface CacheEntry<T> {
  value: T
  savedAt: number // epoch ms — alimenta o badge "dados de HH:MM"
}

export interface EvolucaoRascunho {
  id: string
  pacienteId: number
  estabelecimentoId: number
  modelo: string
  texto: string
  anexos: number
  criadoEm: number
}

interface Backend {
  init(): Promise<void>
  cacheSet(key: string, value: unknown): Promise<void>
  cacheGet<T>(key: string): Promise<CacheEntry<T> | null>
  cacheClear(): Promise<void>
  draftSave(d: EvolucaoRascunho): Promise<void>
  draftList(estabelecimentoId: number): Promise<EvolucaoRascunho[]>
  draftRemove(id: string): Promise<void>
  draftClear(): Promise<void>
}

/* ── Backend nativo (SQLite) ── */
class SqliteBackend implements Backend {
  private conn: SQLiteConnection
  private db: SQLiteDBConnection | null = null

  constructor() {
    this.conn = new SQLiteConnection(CapacitorSQLite)
  }

  async init() {
    const ret = await this.conn.checkConnectionsConsistency().catch(() => ({ result: false }))
    const isConn = (await this.conn.isConnection(DB_NAME, false)).result
    this.db =
      ret.result && isConn
        ? await this.conn.retrieveConnection(DB_NAME, false)
        : await this.conn.createConnection(DB_NAME, false, "no-encryption", 1, false)
    await this.db.open()
    await this.db.execute(`
      CREATE TABLE IF NOT EXISTS cache (
        key TEXT PRIMARY KEY,
        value TEXT NOT NULL,
        saved_at INTEGER NOT NULL
      );
      CREATE TABLE IF NOT EXISTS drafts (
        id TEXT PRIMARY KEY,
        paciente_id INTEGER NOT NULL,
        estabelecimento_id INTEGER NOT NULL,
        modelo TEXT NOT NULL,
        texto TEXT NOT NULL,
        anexos INTEGER NOT NULL DEFAULT 0,
        criado_em INTEGER NOT NULL
      );
    `)
  }

  async cacheSet(key: string, value: unknown) {
    await this.db!.run(
      "INSERT OR REPLACE INTO cache (key, value, saved_at) VALUES (?, ?, ?)",
      [key, JSON.stringify(value), Date.now()],
    )
  }

  async cacheGet<T>(key: string): Promise<CacheEntry<T> | null> {
    const r = await this.db!.query("SELECT value, saved_at FROM cache WHERE key = ?", [key])
    const row = r.values?.[0]
    if (!row) return null
    return { value: JSON.parse(row.value) as T, savedAt: row.saved_at }
  }

  async cacheClear() {
    await this.db!.execute("DELETE FROM cache;")
  }

  async draftSave(d: EvolucaoRascunho) {
    // Cifra o campo `texto` (conteúdo clínico) antes de gravar em repouso.
    const textoCifrado = await cifrarTexto(d.texto)
    await this.db!.run(
      "INSERT OR REPLACE INTO drafts (id, paciente_id, estabelecimento_id, modelo, texto, anexos, criado_em) VALUES (?,?,?,?,?,?,?)",
      [d.id, d.pacienteId, d.estabelecimentoId, d.modelo, textoCifrado, d.anexos, d.criadoEm],
    )
  }

  async draftList(estabelecimentoId: number): Promise<EvolucaoRascunho[]> {
    const r = await this.db!.query(
      "SELECT * FROM drafts WHERE estabelecimento_id = ? ORDER BY criado_em DESC",
      [estabelecimentoId],
    )
    const rows = r.values || []
    const resultado: EvolucaoRascunho[] = []
    for (const row of rows) {
      // Decifra o texto ao ler; em caso de erro (chave trocada), descarta o draft.
      let texto = ""
      try {
        texto = await decifrarTexto(row.texto)
      } catch {
        await this.db!.run("DELETE FROM drafts WHERE id = ?", [row.id])
        continue
      }
      resultado.push({
        id: row.id,
        pacienteId: row.paciente_id,
        estabelecimentoId: row.estabelecimento_id,
        modelo: row.modelo,
        texto,
        anexos: row.anexos,
        criadoEm: row.criado_em,
      })
    }
    return resultado
  }

  async draftRemove(id: string) {
    await this.db!.run("DELETE FROM drafts WHERE id = ?", [id])
  }

  async draftClear() {
    await this.db!.execute("DELETE FROM drafts;")
  }
}

/* ── Fallback web (localStorage) ── */
class WebBackend implements Backend {
  // Rascunhos em memória no web — não persistidos em localStorage (LGPD: sem PII clínica em texto puro no browser).
  private _drafts: EvolucaoRascunho[] = []

  private k(suffix: string) {
    return `imedto.local.${suffix}`
  }
  async init() {}
  async cacheSet(key: string, value: unknown) {
    localStorage.setItem(this.k("cache." + key), JSON.stringify({ value, savedAt: Date.now() }))
  }
  async cacheGet<T>(key: string): Promise<CacheEntry<T> | null> {
    const raw = localStorage.getItem(this.k("cache." + key))
    return raw ? (JSON.parse(raw) as CacheEntry<T>) : null
  }
  async cacheClear() {
    Object.keys(localStorage)
      .filter((k) => k.startsWith(this.k("cache.")))
      .forEach((k) => localStorage.removeItem(k))
  }
  async draftSave(d: EvolucaoRascunho) {
    this._drafts = this._drafts.filter((x) => x.id !== d.id)
    this._drafts.push(d)
  }
  async draftList(estabelecimentoId: number) {
    return this._drafts
      .filter((d) => d.estabelecimentoId === estabelecimentoId)
      .sort((a, b) => b.criadoEm - a.criadoEm)
  }
  async draftRemove(id: string) {
    this._drafts = this._drafts.filter((d) => d.id !== id)
  }

  async draftClear() {
    this._drafts = []
  }
}

const backend: Backend = Capacitor.isNativePlatform() ? new SqliteBackend() : new WebBackend()

let initialized: Promise<void> | null = null
export function initDb(): Promise<void> {
  if (!initialized) initialized = backend.init()
  return initialized
}

// TTL padrão: 24 horas em ms. Entradas mais antigas que isso são descartadas ao ler.
const CACHE_TTL_MS = 24 * 60 * 60 * 1000

export const localDb = {
  async cacheSet(key: string, value: unknown) {
    await initDb()
    return backend.cacheSet(key, value)
  },
  /** Retorna o valor em cache ou null se não existir ou estiver expirado (> 24h). */
  async cacheGet<T>(key: string) {
    await initDb()
    const entry = await backend.cacheGet<T>(key)
    if (!entry) return null
    if (Date.now() - entry.savedAt > CACHE_TTL_MS) return null
    return entry
  },
  async cacheClear() {
    await initDb()
    return backend.cacheClear()
  },
  async draftSave(d: EvolucaoRascunho) {
    await initDb()
    return backend.draftSave(d)
  },
  async draftList(estabelecimentoId: number) {
    await initDb()
    return backend.draftList(estabelecimentoId)
  },
  async draftRemove(id: string) {
    await initDb()
    return backend.draftRemove(id)
  },
  async draftClear() {
    await initDb()
    return backend.draftClear()
  },
}
