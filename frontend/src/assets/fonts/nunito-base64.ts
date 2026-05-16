// Strings base64 das fontes Nunito (subset latin).
// Carregadas via `?raw` do Vite — viram chunk separado do bundle principal.
// Total ~157KB (3 × ~52KB). Importadas apenas pelo helper `usePdfHeader.ts`,
// que é lazy-load junto do jsPDF.
//
// LICENÇA: Nunito — SIL Open Font License 1.1
// https://github.com/googlefonts/nunito/blob/main/OFL.txt

import regularB64 from "./nunito-regular.b64.txt?raw"
import semiBoldB64 from "./nunito-semibold.b64.txt?raw"
import boldB64 from "./nunito-bold.b64.txt?raw"

export const NUNITO_REGULAR_B64: string = regularB64
export const NUNITO_SEMIBOLD_B64: string = semiBoldB64
export const NUNITO_BOLD_B64: string = boldB64
