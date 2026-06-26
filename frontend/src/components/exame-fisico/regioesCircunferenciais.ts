/**
 * Módulo compartilhado — fonte única de verdade para mapeamentos circunferenciais do mapa corporal.
 * Consumido por RegionSelectorPopup.vue (resolver filhos) e SecaoExameFisico.vue (acender vistas).
 *
 * Fusão estrutural do tronco (briefing 2026-06-25_002):
 * - torax/abdome/pelve-circunferencial removidos; substituídos por tronco-circunferencial.
 * - tronco-circunferencial é simétrico: anterior=tronco-anterior, posterior=tronco-posterior.
 * - A exceção clínica abdome-circunferencial → lombossacra-posterior deixa de existir.
 * - PARTE_PARA_TRONCO removido (tronco agora é região real, não pseudo-hotspot sintético).
 */

/**
 * Mapa determinístico: {base}-circunferencial → (ramoAnterior, ramoPosterior).
 * Tronco: simétrico (tronco-anterior + tronco-posterior), sem exceção clínica.
 */
export const RAMOS_CIRCUNFERENCIAL: Record<string, { anterior: string; posterior: string }> = {
  'cabeca-circunferencial':  { anterior: 'cabeca-anterior',  posterior: 'cabeca-posterior'  },
  'pescoco-circunferencial': { anterior: 'pescoco-anterior', posterior: 'pescoco-posterior' },
  'tronco-circunferencial':  { anterior: 'tronco-anterior',  posterior: 'tronco-posterior'  },
  'membro-superior-direito-circunferencial':  { anterior: 'membro-superior-direito-anterior',  posterior: 'membro-superior-direito-posterior'  },
  'membro-superior-esquerdo-circunferencial': { anterior: 'membro-superior-esquerdo-anterior', posterior: 'membro-superior-esquerdo-posterior' },
  'membro-inferior-direito-circunferencial':  { anterior: 'membro-inferior-direito-anterior',  posterior: 'membro-inferior-direito-posterior'  },
  'membro-inferior-esquerdo-circunferencial': { anterior: 'membro-inferior-esquerdo-anterior', posterior: 'membro-inferior-esquerdo-posterior' },
}
