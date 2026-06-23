/**
 * Módulo compartilhado — fonte única de verdade para mapeamentos circunferenciais do mapa corporal.
 * Consumido por RegionSelectorPopup.vue (resolver filhos) e SecaoExameFisico.vue (acender vistas).
 */

/**
 * Mapa determinístico: {base}-circunferencial → (ramoAnterior, ramoPosterior).
 * Exceção clínica: abdome-circunferencial → ramo posterior = lombossacra-posterior.
 */
export const RAMOS_CIRCUNFERENCIAL: Record<string, { anterior: string; posterior: string }> = {
  'cabeca-circunferencial':  { anterior: 'cabeca-anterior',  posterior: 'cabeca-posterior'       },
  'pescoco-circunferencial': { anterior: 'pescoco-anterior', posterior: 'pescoco-posterior'       },
  'torax-circunferencial':   { anterior: 'torax-anterior',   posterior: 'torax-posterior'         },
  'abdome-circunferencial':  { anterior: 'abdome-anterior',  posterior: 'lombossacra-posterior'   }, // exceção clínica
  'pelve-circunferencial':   { anterior: 'pelve-anterior',   posterior: 'pelve-posterior'         },
  'membro-superior-direito-circunferencial':  { anterior: 'membro-superior-direito-anterior',  posterior: 'membro-superior-direito-posterior'  },
  'membro-superior-esquerdo-circunferencial': { anterior: 'membro-superior-esquerdo-anterior', posterior: 'membro-superior-esquerdo-posterior' },
  'membro-inferior-direito-circunferencial':  { anterior: 'membro-inferior-direito-anterior',  posterior: 'membro-inferior-direito-posterior'  },
  'membro-inferior-esquerdo-circunferencial': { anterior: 'membro-inferior-esquerdo-anterior', posterior: 'membro-inferior-esquerdo-posterior' },
}

/**
 * Mapa-only (UI exclusivo, sem correspondente no catálogo): qual polígono de tronco fundido
 * acender para cada nível-1 do tronco.
 * Usado por BodyMap.vue para implementar a regra "OU das partes" — o polígono Tronco (anterior)
 * acende quando qualquer parte anterior estiver examinada, e vice-versa para o posterior.
 */
export const PARTE_PARA_TRONCO: Record<string, string> = {
  'torax-anterior':      'Tronco (anterior)',
  'abdome-anterior':     'Tronco (anterior)',
  'pelve-anterior':      'Tronco (anterior)',
  'torax-posterior':     'Tronco (posterior)',
  'lombossacra-posterior': 'Tronco (posterior)',
  'pelve-posterior':     'Tronco (posterior)',
}
