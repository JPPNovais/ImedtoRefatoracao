<script setup lang="ts">
import type { HTMLAttributes } from "vue"
import type { EmblaCarouselType, EmblaOptionsType, EmblaPluginType } from "embla-carousel"
import { onMounted, onUnmounted, provide, ref } from "vue"
import useEmblaCarousel from "embla-carousel-vue"
import { cn } from "@/utils/cn"

const props = withDefaults(defineProps<{
  class?: HTMLAttributes["class"]
  opts?: EmblaOptionsType
  plugins?: EmblaPluginType[]
  orientation?: "horizontal" | "vertical"
}>(), { orientation: "horizontal" })

const emits = defineEmits<{ "init:api": [api: EmblaCarouselType] }>()

const [emblaRef, emblaApi] = useEmblaCarousel(
  { ...props.opts, axis: props.orientation === "horizontal" ? "x" : "y" },
  props.plugins,
)

const canScrollPrev = ref(false)
const canScrollNext = ref(false)

function scrollPrev() { emblaApi.value?.scrollPrev() }
function scrollNext() { emblaApi.value?.scrollNext() }

function onSelect(api: EmblaCarouselType) {
  canScrollPrev.value = api.canScrollPrev()
  canScrollNext.value = api.canScrollNext()
}

provide("carousel", { orientation: props.orientation })

onMounted(() => {
  if (!emblaApi.value) return
  emblaApi.value.on("reInit", onSelect)
  emblaApi.value.on("select", onSelect)
  onSelect(emblaApi.value)
  emits("init:api", emblaApi.value)
})

onUnmounted(() => {
  emblaApi.value?.off("reInit", onSelect)
  emblaApi.value?.off("select", onSelect)
})

defineExpose({ scrollPrev, scrollNext })
</script>

<template>
  <div role="region" aria-roledescription="carousel" :class="cn('relative', props.class)">
    <div ref="emblaRef" class="overflow-hidden">
      <div :class="cn('flex', orientation === 'horizontal' ? '-ml-4' : '-mt-4 flex-col')">
        <slot />
      </div>
    </div>
    <slot name="controls" :scroll-prev="scrollPrev" :scroll-next="scrollNext" :can-scroll-prev="canScrollPrev" :can-scroll-next="canScrollNext" />
  </div>
</template>
