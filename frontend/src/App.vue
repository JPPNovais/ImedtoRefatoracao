<script setup lang="ts">
import { computed } from "vue"
import { useRoute } from "vue-router"
import AppLayout from "@/layouts/AppLayout.vue"

const route = useRoute()
const usaLayout = computed(() => route.meta.layout === "app")
</script>

<template>
    <AppLayout v-if="usaLayout">
        <router-view />
    </AppLayout>
    <router-view v-else />
</template>
