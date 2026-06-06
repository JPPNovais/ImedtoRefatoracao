/* ─────────────────────────────────────────────────────────────
   Persistência LOCAL da aplicação (SQLite) — cache offline leve e
   rascunhos de evolução, conforme §7 do design brief.
   NÃO substitui o backend: é só cache/draft do device. Nenhuma
   regra de negócio vive aqui; a fonte da verdade é a API.

   - Nativo (iOS/Android): @capacitor-community/sqlite.
   - Web (dev): fallback em localStorage com a MESMA API, para o
     app rodar no navegador sem o runtime wasm do jeep-sqlite.
   ───────────────────────────────────────────────────────────── */
import { Capacitor } from "@capacitor/core"
import {
  CapacitorSQLite,
  SQLiteConnection,
  type SQLiteDBConnection,
} from "@capacitor-community/sqlite"

const DB_NAME = "imedto_local"

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
    await this.db!.run(
      "INSERT OR REPLACE INTO drafts (id, paciente_id, estabelecimento_id, modelo, texto, anexos, criado_em) VALUES (?,?,?,?,?,?,?)",
      [d.id, d.pacienteId, d.estabelecimentoId, d.modelo, d.texto, d.anexos, d.criadoEm],
    )
  }

  async draftList(estabelecimentoId: number): Promise<EvolucaoRascunho[]> {
    const r = await this.db!.query(
      "SELECT * FROM drafts WHERE estabelecimento_id = ? ORDER BY criado_em DESC",
      [estabelecimentoId],
    )
    return (r.values || []).map((row) => ({
      id: row.id,
      pacienteId: row.paciente_id,
      estabelecimentoId: row.estabelecimento_id,
      modelo: row.modelo,
      texto: row.texto,
      anexos: row.anexos,
      criadoEm: row.criado_em,
    }))
  }

  async draftRemove(id: string) {
    await this.db!.run("DELETE FROM drafts WHERE id = ?", [id])
  }
}

/* ── Fallback web (localStorage) ── */
class WebBackend implements Backend {
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
  private draftsKey() {
    return this.k("drafts")
  }
  private allDrafts(): EvolucaoRascunho[] {
    const raw = localStorage.getItem(this.draftsKey())
    return raw ? (JSON.parse(raw) as EvolucaoRascunho[]) : []
  }
  async draftSave(d: EvolucaoRascunho) {
    const all = this.allDrafts().filter((x) => x.id !== d.id)
    all.push(d)
    localStorage.setItem(this.draftsKey(), JSON.stringify(all))
  }
  async draftList(estabelecimentoId: number) {
    return this.allDrafts()
      .filter((d) => d.estabelecimentoId === estabelecimentoId)
      .sort((a, b) => b.criadoEm - a.criadoEm)
  }
  async draftRemove(id: string) {
    localStorage.setItem(
      this.draftsKey(),
      JSON.stringify(this.allDrafts().filter((d) => d.id !== id)),
    )
  }
}

const backend: Backend = Capacitor.isNativePlatform() ? new SqliteBackend() : new WebBackend()

let initialized: Promise<void> | null = null
export function initDb(): Promise<void> {
  if (!initialized) initialized = backend.init()
  return initialized
}

export const localDb = {
  async cacheSet(key: string, value: unknown) {
    await initDb()
    return backend.cacheSet(key, value)
  },
  async cacheGet<T>(key: string) {
    await initDb()
    return backend.cacheGet<T>(key)
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
}
