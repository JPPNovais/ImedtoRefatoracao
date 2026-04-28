import type { VariantProps } from "class-variance-authority"
import { cva } from "class-variance-authority"

export { default as CountBadge } from "./CountBadge.vue"

export const countBadgeVariants = cva(
  "inline-flex items-center justify-center rounded-full px-1.5 py-0.5 text-[10px] font-medium leading-none min-w-[18px]",
  {
    variants: {
      variant: {
        default: "bg-primary/20 text-primary",
        info:    "bg-info/20 text-info",
        warning: "bg-warning/20 text-warning",
        error:   "bg-error text-white",
        success: "bg-success/20 text-success",
        muted:   "bg-muted text-muted-foreground",
      },
    },
    defaultVariants: { variant: "default" },
  },
)

export type CountBadgeVariants = VariantProps<typeof countBadgeVariants>
