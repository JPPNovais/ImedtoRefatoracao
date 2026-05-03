// Setup global do Vitest. Importado automaticamente antes de cada arquivo de teste.
//
// Pinia: cria um pinia novo por teste (`createTestingPinia` consumido nos arquivos
// que precisam — não global, evita state vazado entre testes).
//
// Suprime warnings de Vue Router quando componentes usam `<router-link>` sem
// um router montado (testes unitários de UI não dependem de navegação real).

import { config } from "@vue/test-utils"

config.global.stubs = {
    "router-link": { template: "<a><slot /></a>" },
    "router-view": { template: "<div><slot /></div>" },
}
