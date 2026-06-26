/**
 * Testes de lógica para coloração por vista do mapa corporal.
 * Atualizado para fusão estrutural do tronco (briefing 2026-06-25_002):
 * - CA37-CA40 (torax/abdome/pelve como partes) REMOVIDOS — essas regiões não existem mais.
 * - PARTE_PARA_TRONCO REMOVIDO — tronco é região real, sem pseudo-hotspot.
 * - CA39-novo: tronco-circunferencial → tronco-anterior + tronco-posterior (simétrico).
 * Cobrem CAs: 30, 31, 41, 42, 43 (regressão) + novos tronco-circunferencial.
 */
import { describe, it, expect } from 'vitest'
import { maleRegionPaths, femaleRegionPaths } from './bodyMapPaths'
import { RAMOS_CIRCUNFERENCIAL } from './regioesCircunferenciais'

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

    // Expansão circunferencial aditiva
    const ramos = RAMOS_CIRCUNFERENCIAL[card.regiao_id]
    if (ramos) {
      ids.add(ramos.anterior)
      ids.add(ramos.posterior)
    }
  }

  return ids
}

// ── CA30/CA31 — fusão: hotspots de tronco existem em M e F, sem clipId ────────

describe('CA30 — Tronco real existe em maleRegionPaths sem clipId', () => {
  it('deve existir tronco-anterior no maleRegionPaths', () => {
    const troncoAnt = maleRegionPaths['tronco-anterior']
    expect(troncoAnt).toBeDefined()
    expect(troncoAnt.clipId).toBeUndefined()
  })

  it('deve existir tronco-posterior no maleRegionPaths', () => {
    const troncoPost = maleRegionPaths['tronco-posterior']
    expect(troncoPost).toBeDefined()
    expect(troncoPost.clipId).toBeUndefined()
  })

  // Regiões de partes de tronco foram removidas do catálogo (briefing 2026-06-25_002)
  it('não deve existir hotspot torax-anterior no maleRegionPaths (parte removida)', () => {
    expect(maleRegionPaths['torax-anterior']).toBeUndefined()
  })

  it('não deve existir hotspot abdome-anterior no maleRegionPaths (parte removida)', () => {
    expect(maleRegionPaths['abdome-anterior']).toBeUndefined()
  })

  it('não deve existir hotspot pelve-anterior no maleRegionPaths (parte removida)', () => {
    expect(maleRegionPaths['pelve-anterior']).toBeUndefined()
  })
})

describe('CA31 — Tronco real existe em femaleRegionPaths sem clipId', () => {
  it('deve existir tronco-anterior em femaleRegionPaths sem clipId', () => {
    const t = femaleRegionPaths['tronco-anterior']
    expect(t).toBeDefined()
    expect(t.clipId).toBeUndefined()
  })

  it('deve existir tronco-posterior em femaleRegionPaths sem clipId', () => {
    const t = femaleRegionPaths['tronco-posterior']
    expect(t).toBeDefined()
    expect(t.clipId).toBeUndefined()
  })
})

// ── Tronco circunferencial (novo pós fusão 2026-06-25_002) ───────────────────

describe('tronco-circunferencial — simétrico: → tronco-anterior + tronco-posterior', () => {
  it('RAMOS_CIRCUNFERENCIAL[tronco-circunferencial].anterior deve ser tronco-anterior', () => {
    expect(RAMOS_CIRCUNFERENCIAL['tronco-circunferencial'].anterior).toBe('tronco-anterior')
  })

  it('RAMOS_CIRCUNFERENCIAL[tronco-circunferencial].posterior deve ser tronco-posterior', () => {
    expect(RAMOS_CIRCUNFERENCIAL['tronco-circunferencial'].posterior).toBe('tronco-posterior')
  })

  it('computarMapaIds com tronco-circunferencial deve incluir tronco-anterior e tronco-posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'tronco-circunferencial' }])
    expect(ids.has('tronco-anterior')).toBe(true)
    expect(ids.has('tronco-posterior')).toBe(true)
  })

  it('não deve haver torax/abdome/pelve-circunferencial em RAMOS_CIRCUNFERENCIAL', () => {
    expect(RAMOS_CIRCUNFERENCIAL['torax-circunferencial']).toBeUndefined()
    expect(RAMOS_CIRCUNFERENCIAL['abdome-circunferencial']).toBeUndefined()
    expect(RAMOS_CIRCUNFERENCIAL['pelve-circunferencial']).toBeUndefined()
  })
})

// ── CA41 — circunferencial de membro não toca o tronco ───────────────────────

describe('CA41 — membro-superior-direito-circunferencial acende membro ant+post, sem tocar tronco', () => {
  it('deve conter membro-superior-direito-anterior e membro-superior-direito-posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'membro-superior-direito-circunferencial' }])
    expect(ids.has('membro-superior-direito-anterior')).toBe(true)
    expect(ids.has('membro-superior-direito-posterior')).toBe(true)
  })

  it('não deve conter tronco-anterior nem tronco-posterior', () => {
    const ids = computarMapaIds([{ regiao_id: 'membro-superior-direito-circunferencial' }])
    expect(ids.has('tronco-anterior')).toBe(false)
    expect(ids.has('tronco-posterior')).toBe(false)
  })
})

// ── CA42 — não-regressão bilateral simples = 2 polígonos ─────────────────────

describe('CA42 — bilateral simples acende 2 polígonos (membro-superior-direito-anterior + esquerdo)', () => {
  it('bilateral membro-superior-direito-anterior deve acender o oposto membro-superior-esquerdo-anterior', () => {
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
  it('1 card membro-superior-direito-circunferencial bilateral deve acender 4 polígonos de membro', () => {
    const ids = computarMapaIds([
      { regiao_id: 'membro-superior-direito-circunferencial', lateralidade: 'bilateral' },
    ])
    expect(ids.has('membro-superior-direito-anterior')).toBe(true)
    expect(ids.has('membro-superior-direito-posterior')).toBe(true)
    expect(ids.has('membro-superior-esquerdo-anterior')).toBe(true)
    expect(ids.has('membro-superior-esquerdo-posterior')).toBe(true)
  })

  it('não deve conter tronco para membro-superior-direito-circunferencial bilateral', () => {
    const ids = computarMapaIds([
      { regiao_id: 'membro-superior-direito-circunferencial', lateralidade: 'bilateral' },
    ])
    expect(ids.has('tronco-anterior')).toBe(false)
    expect(ids.has('tronco-posterior')).toBe(false)
  })
})

// ── RAMOS_CIRCUNFERENCIAL — todos os 7 nós mapeados (fusão 2026-06-25_002) ────

describe('RAMOS_CIRCUNFERENCIAL — 7 nós circunferenciais mapeados corretamente', () => {
  const esperados = [
    { id: 'cabeca-circunferencial',                    ant: 'cabeca-anterior',                   post: 'cabeca-posterior'                  },
    { id: 'pescoco-circunferencial',                   ant: 'pescoco-anterior',                   post: 'pescoco-posterior'                  },
    { id: 'tronco-circunferencial',                    ant: 'tronco-anterior',                    post: 'tronco-posterior'                   },
    { id: 'membro-superior-direito-circunferencial',   ant: 'membro-superior-direito-anterior',   post: 'membro-superior-direito-posterior'  },
    { id: 'membro-superior-esquerdo-circunferencial',  ant: 'membro-superior-esquerdo-anterior',  post: 'membro-superior-esquerdo-posterior' },
    { id: 'membro-inferior-direito-circunferencial',   ant: 'membro-inferior-direito-anterior',   post: 'membro-inferior-direito-posterior'  },
    { id: 'membro-inferior-esquerdo-circunferencial',  ant: 'membro-inferior-esquerdo-anterior',  post: 'membro-inferior-esquerdo-posterior' },
  ]

  for (const { id, ant, post } of esperados) {
    it(`${id} → anterior: ${ant}, posterior: ${post}`, () => {
      expect(RAMOS_CIRCUNFERENCIAL[id].anterior).toBe(ant)
      expect(RAMOS_CIRCUNFERENCIAL[id].posterior).toBe(post)
    })
  }

  it('deve ter exatamente 7 entradas (torax/abdome/pelve removidos)', () => {
    expect(Object.keys(RAMOS_CIRCUNFERENCIAL).length).toBe(7)
  })
})
