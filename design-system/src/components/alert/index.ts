import type { VariantProps } from "class-variance-authority"
import { cva } from "class-variance-authority"

export { default as Alert } from "./Alert.vue"
export { default as AlertTitle } from "./AlertTitle.vue"
export { default as AlertDescription } from "./AlertDescription.vue"

export const alertVariants = cva(
  "relative w-full rounded-lg border bg-background px-4 py-3 text-sm [&>svg+div]:translate-y-[-3px] [&>svg]:absolute [&>svg]:left-4 [&>svg]:top-4 [&>svg]:text-foreground [&>svg~*]:pl-7",
  {
    variants: {
      variant: {
        default: "text-foreground border-border",
        destructive:
          "border-destructive/50 text-destructive dark:border-destructive [&>svg]:text-destructive",
        success:
          "border-success/30 text-success [&>svg]:text-success",
        warning:
          "border-warning/30 text-warning [&>svg]:text-warning",
        error:
          "border-error/30 text-error [&>svg]:text-error",
        info:
          "border-info/30 text-info [&>svg]:text-info",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  },
)

export type AlertVariants = VariantProps<typeof alertVariants>
