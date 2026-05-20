<script setup lang="ts">
/**
 * TermoEditorTipTap — editor WYSIWYG para modelos de termo de consentimento.
 *
 * Stack: TipTap 3 (StarterKit + Underline + Placeholder) + Node custom "variavel"
 * que renderiza `{{paciente.nome}}` como pill colorida (atom, não editável).
 *
 * Contrato:
 *   - v-model recebe e devolve HTML string.
 *   - Sanitização vive no backend (Ganss.Xss); aqui é só UX.
 *
 * O Node "variavel" guarda a chave canônica (`paciente.nome`) no atributo
 * `data-variavel`. Quando serializado pra HTML, vira `<span class="termo-variavel" data-variavel="paciente.nome">{{paciente.nome}}</span>` — o
 * backend continua substituindo via regex `{{...}}` (não depende do atributo).
 *
 * Expor `inserirVariavel(chave)` via defineExpose pra a view-pai (sidebar de
 * variáveis) chamar quando o usuário clica num item da lista.
 */
import { onBeforeUnmount, shallowRef, watch } from "vue"
import { Editor, EditorContent } from "@tiptap/vue-3"
// StarterKit v3 já inclui o mark `underline` — não importamos a extensão
// separada para não disparar o warning de duplicação.
import StarterKit from "@tiptap/starter-kit"
import Placeholder from "@tiptap/extension-placeholder"
import { Node, mergeAttributes } from "@tiptap/core"

const props = withDefaults(defineProps<{
    modelValue: string
    placeholder?: string
    disabled?: boolean
}>(), {
    placeholder: "Digite o conteúdo do termo aqui…",
    disabled: false,
})

const emit = defineEmits<{
    (e: "update:modelValue", v: string): void
}>()

// ─── Node custom: Variavel ────────────────────────────────────────────────
// Atom inline. Não editável internamente — usuário insere/remove inteiro.
// O conteúdo visível é `{{<chave>}}` (mesma string do backend) para que o
// preview funcione mesmo sem o atributo `data-variavel` (defesa em profundidade).
const VariavelNode = Node.create({
    name: "variavel",
    group: "inline",
    inline: true,
    atom: true,
    selectable: true,
    addAttributes() {
        return {
            chave: {
                default: "",
                parseHTML: el => (el as HTMLElement).getAttribute("data-variavel") ?? "",
                renderHTML: attrs => ({ "data-variavel": attrs.chave }),
            },
        }
    },
    parseHTML() {
        return [{ tag: "span.termo-variavel" }]
    },
    renderHTML({ node, HTMLAttributes }) {
        return [
            "span",
            mergeAttributes(HTMLAttributes, { class: "termo-variavel" }),
            `{{${node.attrs.chave}}}`,
        ]
    },
})

const editor = shallowRef<Editor | null>(null)

function criarEditor() {
    editor.value = new Editor({
        content: props.modelValue || "",
        editable: !props.disabled,
        extensions: [
            StarterKit.configure({
                heading: { levels: [2, 3] },
            }),
            Placeholder.configure({ placeholder: props.placeholder }),
            VariavelNode,
        ],
        onUpdate: ({ editor: ed }) => {
            const html = ed.getHTML()
            if (html !== props.modelValue) emit("update:modelValue", html)
        },
    })
}

criarEditor()

// Sincronização externa: se o pai trocar o modelValue (ex: ao carregar modelo
// na edição), reflete no editor sem disparar update reentrante.
watch(() => props.modelValue, (novo) => {
    if (!editor.value) return
    if (novo === editor.value.getHTML()) return
    editor.value.commands.setContent(novo || "", { emitUpdate: false })
})

watch(() => props.disabled, (d) => {
    editor.value?.setEditable(!d)
})

onBeforeUnmount(() => {
    editor.value?.destroy()
    editor.value = null
})

// ─── Toolbar helpers ──────────────────────────────────────────────────────
function ehAtivo(marca: string, attrs?: Record<string, unknown>): boolean {
    return editor.value?.isActive(marca, attrs) ?? false
}
function toggleBold()       { editor.value?.chain().focus().toggleBold().run() }
function toggleItalic()     { editor.value?.chain().focus().toggleItalic().run() }
function toggleUnderline()  { editor.value?.chain().focus().toggleUnderline().run() }
function toggleH2()         { editor.value?.chain().focus().toggleHeading({ level: 2 }).run() }
function toggleH3()         { editor.value?.chain().focus().toggleHeading({ level: 3 }).run() }
function toggleBullet()     { editor.value?.chain().focus().toggleBulletList().run() }
function toggleOrdered()    { editor.value?.chain().focus().toggleOrderedList().run() }
function toggleBlockquote() { editor.value?.chain().focus().toggleBlockquote().run() }
function limparFormat()     { editor.value?.chain().focus().clearNodes().unsetAllMarks().run() }

// ─── API pública ──────────────────────────────────────────────────────────
function inserirVariavel(chave: string) {
    if (!editor.value) return
    // Aceita "{{paciente.nome}}" ou "paciente.nome".
    const limpa = chave.replace(/^\{\{\s*|\s*\}\}$/g, "")
    editor.value
        .chain()
        .focus()
        .insertContent([
            { type: "variavel", attrs: { chave: limpa } },
            { type: "text", text: " " },
        ])
        .run()
}

defineExpose({ inserirVariavel })
</script>

<template>
    <div class="editor-wrapper" :class="{ 'is-disabled': disabled }">
        <div v-if="editor" class="editor-toolbar" role="toolbar" aria-label="Formatação do termo">
            <button type="button" class="tb-btn" :class="{ active: ehAtivo('bold') }" :disabled="disabled"
                title="Negrito (Ctrl+B)" @click="toggleBold"><i class="fa-solid fa-bold" /></button>
            <button type="button" class="tb-btn" :class="{ active: ehAtivo('italic') }" :disabled="disabled"
                title="Itálico (Ctrl+I)" @click="toggleItalic"><i class="fa-solid fa-italic" /></button>
            <button type="button" class="tb-btn" :class="{ active: ehAtivo('underline') }" :disabled="disabled"
                title="Sublinhado (Ctrl+U)" @click="toggleUnderline"><i class="fa-solid fa-underline" /></button>

            <span class="tb-sep" aria-hidden="true" />

            <button type="button" class="tb-btn" :class="{ active: ehAtivo('heading', { level: 2 }) }" :disabled="disabled"
                title="Título H2" @click="toggleH2"><i class="fa-solid fa-heading" /><sub>2</sub></button>
            <button type="button" class="tb-btn" :class="{ active: ehAtivo('heading', { level: 3 }) }" :disabled="disabled"
                title="Título H3" @click="toggleH3"><i class="fa-solid fa-heading" /><sub>3</sub></button>

            <span class="tb-sep" aria-hidden="true" />

            <button type="button" class="tb-btn" :class="{ active: ehAtivo('bulletList') }" :disabled="disabled"
                title="Lista com marcadores" @click="toggleBullet"><i class="fa-solid fa-list-ul" /></button>
            <button type="button" class="tb-btn" :class="{ active: ehAtivo('orderedList') }" :disabled="disabled"
                title="Lista numerada" @click="toggleOrdered"><i class="fa-solid fa-list-ol" /></button>
            <button type="button" class="tb-btn" :class="{ active: ehAtivo('blockquote') }" :disabled="disabled"
                title="Citação" @click="toggleBlockquote"><i class="fa-solid fa-quote-right" /></button>

            <span class="tb-sep" aria-hidden="true" />

            <button type="button" class="tb-btn" :disabled="disabled"
                title="Limpar formatação" @click="limparFormat"><i class="fa-solid fa-broom" /></button>
        </div>

        <EditorContent :editor="editor ?? undefined" class="editor-conteudo" />
    </div>
</template>

<style scoped>
.editor-wrapper {
    border: 1px solid hsl(var(--border));
    border-radius: var(--radius);
    background: white;
    display: flex;
    flex-direction: column;
    overflow: hidden;
}
.editor-wrapper.is-disabled {
    opacity: 0.6;
    pointer-events: none;
}

.editor-toolbar {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 2px;
    padding: 6px 8px;
    background: hsl(var(--muted));
    border-bottom: 1px solid hsl(var(--border));
}
.tb-btn {
    border: none;
    background: transparent;
    color: hsl(var(--foreground));
    width: 30px;
    height: 30px;
    border-radius: 6px;
    display: inline-flex;
    align-items: center;
    justify-content: center;
    cursor: pointer;
    font-size: 13px;
    transition: background 0.12s;
}
.tb-btn:hover:not(:disabled) { background: hsl(var(--accent)); }
.tb-btn.active {
    background: hsl(var(--primary) / 0.15);
    color: hsl(var(--primary));
}
.tb-btn:disabled { cursor: not-allowed; opacity: 0.5; }
.tb-btn sub { font-size: 9px; margin-left: 1px; }

.tb-sep {
    width: 1px;
    height: 20px;
    background: hsl(var(--border));
    margin: 0 4px;
}

.editor-conteudo {
    flex: 1;
    overflow-y: auto;
    min-height: 420px;
    padding: 16px 20px;
    font-size: 14px;
    line-height: 1.6;
}
</style>

<!--
  Estilos globais: o ProseMirror renderiza o conteúdo dentro do componente
  mas seus filhos não são automaticamente scoped (são gerados dinamicamente).
  Para que H2/blockquote/lista funcionem visualmente, é preciso CSS não-scoped.
-->
<style>
.editor-conteudo .ProseMirror {
    outline: none;
    min-height: 380px;
}
.editor-conteudo .ProseMirror p { margin: 0 0 0.6em; }
.editor-conteudo .ProseMirror h2 { font-size: 1.25em; font-weight: 700; margin: 1em 0 0.4em; }
.editor-conteudo .ProseMirror h3 { font-size: 1.08em; font-weight: 700; margin: 0.8em 0 0.4em; }
.editor-conteudo .ProseMirror ul,
.editor-conteudo .ProseMirror ol { padding-left: 1.4em; margin: 0 0 0.6em; }
.editor-conteudo .ProseMirror blockquote {
    border-left: 3px solid hsl(var(--primary) / 0.4);
    padding-left: 12px;
    color: hsl(var(--muted-foreground));
    margin: 0.6em 0;
}
.editor-conteudo .ProseMirror p.is-editor-empty:first-child::before {
    content: attr(data-placeholder);
    color: hsl(var(--muted-foreground));
    float: left;
    pointer-events: none;
    height: 0;
}

/* Chip de variável — usado pelo editor E pelo preview (estilo global). */
.termo-variavel {
    display: inline-block;
    padding: 1px 8px;
    margin: 0 1px;
    background: hsl(var(--primary) / 0.12);
    color: hsl(var(--primary));
    border: 1px solid hsl(var(--primary) / 0.25);
    border-radius: 999px;
    font-size: 0.85em;
    font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
    user-select: all;
    white-space: nowrap;
}
.ProseMirror-selectednode .termo-variavel {
    background: hsl(var(--primary) / 0.25);
    outline: 2px solid hsl(var(--primary));
}
</style>
