import { http } from "@/lib/http"
import type { DashboardDto } from "@/types"

export const dashboardService = {
  async obter(): Promise<DashboardDto> {
    return http.get("/dashboard")
  },
}
