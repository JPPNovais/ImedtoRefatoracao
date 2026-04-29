<script setup lang="ts">
/**
 * AppCheckbox — checkbox acessível padronizado do design system.
 * Wrap do input[type=checkbox] nativo com estilo do produto.
 */
interface Props {
    modelValue?: boolean
    label?: string
    disabled?: boolean
    id?: string
}

const props = withDefaults(defineProps<Props>(), {
    modelValue: false,
    disabled: false,
})

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    change: [value: boolean]
}>()

function handleChange(e: Event) {
    const checked = (e.target as HTMLInputElement).checked
    emit("update:modelValue", checked)
    emit("change", checked)
}
</script>

<template>
    <label class="checkbox-wrapper" :class="{ 'checkbox-wrapper--disabled': disabled }">
        <input
            :id="id"
            type="checkbox"
            class="checkbox-input"
            :checked="modelValue"
            :disabled="disabled"
            :aria-checked="modelValue"
            @change="handleChange"
        />
        <span class="checkbox-box" aria-hidden="true">
            <i v-if="modelValue" class="fa-solid fa-check checkbox-check"></i>
        </span>
        <span v-if="label" class="checkbox-label">{{ label }}</span>
        <slot v-else />
    </label>
</template>

<style scoped>
.checkbox-wrapper {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    cursor: pointer;
    user-select: none;
    font-size: 0.9em;
    color: hsl(var(--foreground));
}

.checkbox-wrapper--disabled {
    opacity: 0.5;
    cursor: not-allowed;
    pointer-events: none;
}

.checkbox-input {
    position: absolute;
    width: 1px;
    height: 1px;
    padding: 0;
    margin: -1px;
    overflow: hidden;
    clip: rect(0, 0, 0, 0);
    white-space: nowrap;
    border-width: 0;
}

.checkbox-box {
    width: 18px;
    height: 18px;
    border-radius: var(--radius-sm);
    border: 2px solid hsl(var(--border));
    background: hsl(var(--background));
    display: flex;
    align-items: center;
    justify-content: center;
    flex-shrink: 0;
    transition: background 0.12s, border-color 0.12s;
}

.checkbox-input:checked ~ .checkbox-box {
    background: hsl(var(--primary));
    border-color: hsl(var(--primary));
}

.checkbox-input:focus-visible ~ .checkbox-box {
    box-shadow: 0 0 0 2px hsl(var(--ring) / 0.4);
}

.checkbox-check {
    font-size: 0.65em;
    color: hsl(var(--primary-foreground));
}

.checkbox-label {
    line-height: 1.4;
}
</style>
