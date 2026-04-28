<script setup lang="ts" generic="TData, TValue">
import type { ColumnDef, SortingState, ColumnFiltersState, VisibilityState } from "@tanstack/vue-table"
import type { HTMLAttributes } from "vue"
import { ref } from "vue"
import { FlexRender, getCoreRowModel, getSortedRowModel, getFilteredRowModel, getPaginationRowModel, useVueTable } from "@tanstack/vue-table"
import { cn } from "@/utils/cn"
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/table"

const props = withDefaults(defineProps<{
  class?: HTMLAttributes["class"]
  columns: ColumnDef<TData, TValue>[]
  data: TData[]
  enableSorting?: boolean
  enableFiltering?: boolean
  enablePagination?: boolean
}>(), { enableSorting: true, enableFiltering: false, enablePagination: false })

const sorting = ref<SortingState>([])
const columnFilters = ref<ColumnFiltersState>([])
const columnVisibility = ref<VisibilityState>({})

const table = useVueTable({
  get data() { return props.data },
  get columns() { return props.columns },
  getCoreRowModel: getCoreRowModel(),
  getSortedRowModel: props.enableSorting ? getSortedRowModel() : undefined,
  getFilteredRowModel: props.enableFiltering ? getFilteredRowModel() : undefined,
  getPaginationRowModel: props.enablePagination ? getPaginationRowModel() : undefined,
  onSortingChange: u => { sorting.value = typeof u === "function" ? u(sorting.value) : u },
  onColumnFiltersChange: u => { columnFilters.value = typeof u === "function" ? u(columnFilters.value) : u },
  onColumnVisibilityChange: u => { columnVisibility.value = typeof u === "function" ? u(columnVisibility.value) : u },
  state: {
    get sorting() { return sorting.value },
    get columnFilters() { return columnFilters.value },
    get columnVisibility() { return columnVisibility.value },
  },
})

defineExpose({ table })
</script>

<template>
  <div :class="cn('w-full', props.class)">
    <slot name="toolbar" :table="table" />
    <div class="rounded-md border">
      <Table>
        <TableHeader>
          <TableRow v-for="hg in table.getHeaderGroups()" :key="hg.id">
            <TableHead v-for="h in hg.headers" :key="h.id">
              <FlexRender v-if="!h.isPlaceholder" :render="h.column.columnDef.header" :props="h.getContext()" />
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <template v-if="table.getRowModel().rows?.length">
            <TableRow v-for="row in table.getRowModel().rows" :key="row.id" :data-state="row.getIsSelected() ? 'selected' : undefined">
              <TableCell v-for="cell in row.getVisibleCells()" :key="cell.id">
                <FlexRender :render="cell.column.columnDef.cell" :props="cell.getContext()" />
              </TableCell>
            </TableRow>
          </template>
          <template v-else>
            <TableRow>
              <TableCell :colspan="columns.length" class="h-24 text-center">
                <slot name="empty">Nenhum resultado encontrado.</slot>
              </TableCell>
            </TableRow>
          </template>
        </TableBody>
      </Table>
    </div>
    <slot name="pagination" :table="table" />
  </div>
</template>
