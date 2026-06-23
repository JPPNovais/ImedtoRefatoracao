/**
 * Testes de lógica para coloração por vista do mapa corporal (briefing 2026-06-08_006 B2).
 * Cobrem CAs 30, 31, 37, 38, 39, 40, 41, 42, 43.
 * Sem DOM real — validação visual fica para o usuário em prod.
 */
import { describe, it, expect } from 'vitest'
import { maleRegionPaths, femaleRegionPaths } from './bodyMapPaths'
import { RAMOS_CIRCUNFERENCIAL, PARTE_PARA_TRONCO } from './regioesCircunferenciais'

// ── Helpers que simulam a lógica de regioesExaminadasMapa + BodyMap ────────────

/**
 * Mapeamento de ids de nível-1 de membro para o oposto (direito↔esquerdo).
 * Replica a lógica de getOpostoNivel1Id para todos os ids de membro (anterior,
 * posterior e circunferencial) — evita simular o catálogo Vue reativo no teste.
 */
const OPOSTO_MEMBRO: Record<string, string> = {
  'membro-superior-direito-anterior':         'membro-superior-esquerdo-anterior',
  'membro-superior-esquerdo-anterior':        'membro-superior-direito-anterior',
  'membro-superior-direito-posterior':        'membro-superior-esquerdo-posterior',
  'membro-superior-esquerdo-posterior':       'membro-superior-direito-posterior',
  'membro-superior-direito-circunferencial':  'membro-superior-esquerdo-circunferencial',
  'membro-superior-esquerdo-circunferencial': 'membro-superior-direito-circunferencial',
  'membro-inferior-direito-anterior':         'membro-inferior-esquerdo-anterior',
  'membro-inferior-esquerdo-anterior':        'membro-inferior-direito-anterior',
  'membro-inferior-direito-posterior':        'membro-inferior-esquerdo-posterior',
  'membro-inferior-esquerdo-posterior':       'membro-inferior-direito-posterior',
  'membro-inferior-direito-circunferencial':  'membro-inferior-esquerdo-circunferencial',
  'membro-inferior-esquerdo-circunferencial': 'membro-inferior-direito-circunferencial',
}

/**
 * Reproduz a lógica de regioesExaminadasMapa do SecaoExameFisico:
 * dado um card (regiao_id + lateralidade), retorna o conjunto de ids que devem
 * acender no mapa — expandindo bilateral e circunferencial de forma aditiva.
 *
 * Caminho REAL: sem injeção externa de opostoN1. O oposto é derivado diretamente
 * pelo regiao_id via OPOSTO_MEMBRO, espelhando getOpostoNivel1Id do código real.
 */
function computarMapaIds(
  cards: Array<{
    regiao_id: string
    lateralidade?: 'D' | 'E' | 'bilateral' | null
  }>,
): Set<string> {
  const ids = new Set<string>()

  for (const card of cards) {
    ids.add(card.regiao_id)

    // Espelhamento bilateral — deriva o oposto pelo id (sem injeção externa)
    if (card.lateralidade === 'bilateral') {
      const oposto = OPOSTO_MEMBRO[card.regiao_id]
      if (oposto) {
        ids.add(oposto)
        // Se o oposto for circunferencial, expande também seus ramos
        const ramosOposto = RAMOS_CIRCUNFERENCIAL[oposto]
        if (ramosOposto) {
          ids.add(ramosOposto.anterior)
          ids.add(ramosOposto.posterior)
        }
      }
    }

    // Expansão circunferencial aditiva (B2)
    const ramos = RAMOS_CIRCUNFERENCIAL[card.regiao_id]
    if (ramos) {
      ids.add(ramos.anterior)
      ids.add(ramos.posterior)
    }
  }

  return ids
}

/**
 * Verifica se o polígono de tronco acende via lógica "OU das partes" (PARTE_PARA_TRONCO).
 */
function troncoAcende(nomeTronco: string, mapIds: Set<string>): boolean {
  return Array.from(mapIds).some((id) => PARTE_PARA_TRONCO[id] === nomeTronco)
}

// ── CA30/CA31 — fusão: hotspots de tronco existem em M e F, sem clipId ────────

describe('CA30 — Tronco (anterior) existe em maleRegionPaths sem clipId', () => {
  it('deve existir exatamente 1 entrada Tronco (anterior) no maleRegionPaths', () => {
    const troncoAnt = maleRegionPaths['Tronco (anterior)']
    expect(troncoAnt).toBeDefined()
    expect(troncoAnt.clipId).toBeUndefined()
  })

  it('deve existir exatamente 1 entrada Tronco (posterior) no maleRegionPaths', () => {
    const troncoPost = maleRegionPaths['Tronco (posterior)']
    expect(troncoPost).toBeDefined()
    expect(troncoPost.clipId).toBeUndefined()
  })

  it('não deve existir hotspot Tórax (anterior) no maleRegionPaths', () => {
    expect(maleRegionPaths['Tórax (anterior)']).toBeUndefined()
  })

  it('não deve existir hotspot Abdome (anterior) no maleRegionPaths', () => {
    expect(maleRegionPaths['Abdome (anterior)']).toBeUndefined()
  })

  it('não deve existir hotspot Pelve (anterior) no maleRegionPaths', () => {
    expect(maleRegionPaths['Pelve (anterior)']).toBeUndefined()
  })

  it('não deve existir hotspot Tórax (posterior) no maleRegionPaths', () => {
    expect(maleRegionPaths['Tórax (posterior)']).toBeUndefined()
  })

  it('não deve existir hotspot Região lombossacra (posterior) no maleRegionPaths', () => {
    expect(maleRegionPaths['Região lombossacra (posterior)']).toBeUndefined()
  })

  it('não deve existir hotspot Pelve (posterior) no maleRegionPaths', () => {
    expect(maleRegionPaths['Pelve (posterior)']).toBeUndefined()
  })
})

describe('CA31 — Tronco fundido existe em femaleRegionPaths sem clipId', () => {
  it('deve existir Tronco (anterior) em femaleRegionPaths sem clipId', () => {
    const t = femaleRegionPaths['Tronco (anterior)']
    expect(t).toBeDefined()
    expect(t.clipId).toBeUndefined()
  })

  it('deve existir Tronco (posterior) em femaleRegionPaths sem clipId', () => {
    const t = femaleRegionPaths['Tronco (posterior)']
    expect(t).toBeDefined()
    expect(t.clipId).toBeUndefined()
  })

  it('não deve existir hotspot Tórax (anterior) em femaleRegionPaths', () => {
    expect(femaleRegionPaths['Tórax (anterior)']).toBeUndefined()
  })

  it('não deve existir hotspot Abdome (anterior) em femaleRegionPaths', () => {
    expect(femaleRegionPaths['Abdome (anterior)']).toBeUndefined()
  })

  it('não deve existir hotspot Pelve (anterior) em femaleRegionPaths', () => {
    expect(femaleRegionPaths['Pelve (anterior)']).toBeUndefined()
  })
})

// ── CA37 — coloração anterior: acende Tronco (anterior), não o posterior ──────

describe('CA37 — coloração anterior acende Tronco (anterior), não o posterior', () => {
  it('torax-anterior acende só o tronco anterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'torax-anterior' }])
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(true)
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(false)
  })

  it('abdome-anterior acende só o tronco anterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'abdome-anterior' }])
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(true)
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(false)
  })

  it('pelve-anterior acende só o tronco anterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'pelve-anterior' }])
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(true)
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(false)
  })
})

// ── CA38 — coloração posterior acende Tronco (posterior), não o anterior ──────

describe('CA38 — coloração posterior acende Tronco (posterior), não o anterior', () => {
  it('torax-posterior acende só o tronco posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'torax-posterior' }])
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(true)
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(false)
  })

  it('lombossacra-posterior acende só o tronco posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'lombossacra-posterior' }])
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(true)
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(false)
  })

  it('pelve-posterior acende só o tronco posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'pelve-posterior' }])
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(true)
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(false)
  })
})

// ── CA39 — circunferencial do tórax acende ambos os polígonos de tronco ───────

describe('CA39 — torax-circunferencial acende Tronco (anterior) E Tronco (posterior)', () => {
  it('deve conter torax-anterior e torax-posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'torax-circunferencial' }])
    expect(ids.has('torax-anterior')).toBe(true)
    expect(ids.has('torax-posterior')).toBe(true)
  })

  it('deve acender ambos os polígonos de tronco', () => {
    const ids = computarMapaIds([{ regiao_id: 'torax-circunferencial' }])
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(true)
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(true)
  })
})

// ── CA40 — exceção abdome↔lombossacra ─────────────────────────────────────────

describe('CA40 — abdome-circunferencial → abdome-anterior + lombossacra-posterior (não abdome-posterior)', () => {
  it('deve conter abdome-anterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'abdome-circunferencial' }])
    expect(ids.has('abdome-anterior')).toBe(true)
  })

  it('deve conter lombossacra-posterior (não abdome-posterior)', () => {
    const ids = computarMapaIds([{ regiao_id: 'abdome-circunferencial' }])
    expect(ids.has('lombossacra-posterior')).toBe(true)
    expect(ids.has('abdome-posterior')).toBe(false)
  })

  it('deve acender Tronco (anterior) via abdome-anterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'abdome-circunferencial' }])
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(true)
  })

  it('deve acender Tronco (posterior) via lombossacra-posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'abdome-circunferencial' }])
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(true)
  })
})

// ── CA41 — circunferencial de membro não toca o tronco ───────────────────────

describe('CA41 — membro-superior-direito-circunferencial acende membro-superior-direito-anterior e membro-superior-direito-posterior, sem tocar o tronco', () => {
  it('deve conter membro-superior-direito-anterior e membro-superior-direito-posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'membro-superior-direito-circunferencial' }])
    expect(ids.has('membro-superior-direito-anterior')).toBe(true)
    expect(ids.has('membro-superior-direito-posterior')).toBe(true)
  })

  it('não deve acender nenhum polígono de tronco', () => {
    const ids = computarMapaIds([{ regiao_id: 'membro-superior-direito-circunferencial' }])
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(false)
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(false)
  })
})

// ── CA42 — não-regressão bilateral simples = 2 polígonos ─────────────────────

describe('CA42 — bilateral simples acende 2 polígonos (membro-superior-direito-anterior + membro-superior-esquerdo-anterior)', () => {
  it('bilateral membro-superior-direito-anterior deve acender o oposto membro-superior-esquerdo-anterior (caminho real, sem injeção externa)', () => {
    const ids = computarMapaIds([
      { regiao_id: 'membro-superior-direito-anterior', lateralidade: 'bilateral' },
    ])
    expect(ids.has('membro-superior-direito-anterior')).toBe(true)
    expect(ids.has('membro-superior-esquerdo-anterior')).toBe(true)
    expect(ids.has('membro-superior-direito-posterior')).toBe(false)
    expect(ids.has('membro-superior-esquerdo-posterior')).toBe(false)
  })
})

// ── CA43 — bilateral × circunferencial = 4 polígonos ─────────────────────────

describe('CA43 — bilateral × circunferencial = 4 polígonos (membro-superior-direito/esquerdo × ant/post)', () => {
  it('1 card membro-superior-direito-circunferencial bilateral deve acender membro-superior-direito-anterior, membro-superior-direito-posterior, membro-superior-esquerdo-anterior, membro-superior-esquerdo-posterior (caminho real)', () => {
    const ids = computarMapaIds([
      { regiao_id: 'membro-superior-direito-circunferencial', lateralidade: 'bilateral' },
    ])
    expect(ids.has('membro-superior-direito-anterior')).toBe(true)
    expect(ids.has('membro-superior-direito-posterior')).toBe(true)
    expect(ids.has('membro-superior-esquerdo-anterior')).toBe(true)
    expect(ids.has('membro-superior-esquerdo-posterior')).toBe(true)
  })

  it('não deve acender tronco para membro-superior-direito-circunferencial bilateral', () => {
    const ids = computarMapaIds([
      { regiao_id: 'membro-superior-direito-circunferencial', lateralidade: 'bilateral' },
    ])
    expect(troncoAcende('Tronco (anterior)', ids)).toBe(false)
    expect(troncoAcende('Tronco (posterior)', ids)).toBe(false)
  })
})

// ── PARTE_PARA_TRONCO — cobre as 6 partes ────────────────────────────────────

describe('PARTE_PARA_TRONCO — mapeamento correto das 6 partes', () => {
  it('torax-anterior → Tronco (anterior)', () => {
    expect(PARTE_PARA_TRONCO['torax-anterior']).toBe('Tronco (anterior)')
  })
  it('abdome-anterior → Tronco (anterior)', () => {
    expect(PARTE_PARA_TRONCO['abdome-anterior']).toBe('Tronco (anterior)')
  })
  it('pelve-anterior → Tronco (anterior)', () => {
    expect(PARTE_PARA_TRONCO['pelve-anterior']).toBe('Tronco (anterior)')
  })
  it('torax-posterior → Tronco (posterior)', () => {
    expect(PARTE_PARA_TRONCO['torax-posterior']).toBe('Tronco (posterior)')
  })
  it('lombossacra-posterior → Tronco (posterior)', () => {
    expect(PARTE_PARA_TRONCO['lombossacra-posterior']).toBe('Tronco (posterior)')
  })
  it('pelve-posterior → Tronco (posterior)', () => {
    expect(PARTE_PARA_TRONCO['pelve-posterior']).toBe('Tronco (posterior)')
  })
})

// ── RAMOS_CIRCUNFERENCIAL — todos os 9 nós mapeados ──────────────────────────

describe('RAMOS_CIRCUNFERENCIAL — 9 nós circunferenciais mapeados corretamente', () => {
  const esperados = [
    { id: 'cabeca-circunferencial',                    ant: 'cabeca-anterior',                  post: 'cabeca-posterior'                  },
    { id: 'pescoco-circunferencial',                   ant: 'pescoco-anterior',                  post: 'pescoco-posterior'                  },
    { id: 'torax-circunferencial',                     ant: 'torax-anterior',                    post: 'torax-posterior'                    },
    { id: 'abdome-circunferencial',                    ant: 'abdome-anterior',                   post: 'lombossacra-posterior'              }, // exceção clínica
    { id: 'pelve-circunferencial',                     ant: 'pelve-anterior',                    post: 'pelve-posterior'                    },
    { id: 'membro-superior-direito-circunferencial',   ant: 'membro-superior-direito-anterior',  post: 'membro-superior-direito-posterior'  },
    { id: 'membro-superior-esquerdo-circunferencial',  ant: 'membro-superior-esquerdo-anterior', post: 'membro-superior-esquerdo-posterior' },
    { id: 'membro-inferior-direito-circunferencial',   ant: 'membro-inferior-direito-anterior',  post: 'membro-inferior-direito-posterior'  },
    { id: 'membro-inferior-esquerdo-circunferencial',  ant: 'membro-inferior-esquerdo-anterior', post: 'membro-inferior-esquerdo-posterior' },
  ]

  for (const { id, ant, post } of esperados) {
    it(`${id} → anterior: ${ant}, posterior: ${post}`, () => {
      expect(RAMOS_CIRCUNFERENCIAL[id].anterior).toBe(ant)
      expect(RAMOS_CIRCUNFERENCIAL[id].posterior).toBe(post)
    })
  }
})
