// src/components/CustomTable/DataTable.tsx

import React from "react"
import {
  ColumnDef,
  ColumnFiltersState,
  SortingState, // 👈 O tipo para o estado de ordenação
  flexRender,
  getCoreRowModel,
  getFilteredRowModel,
  getPaginationRowModel,
  getSortedRowModel, // 👈 O helper para ordenação
  useReactTable,
} from "@tanstack/react-table"

import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table"
import { ArrowUpDown } from "lucide-react" // Ícone para indicar ordenação

// 1. Adicionar 'initialSorting' à interface de props. Ela é opcional.
interface DataTableProps<TData, TValue> {
  columns: ColumnDef<TData, TValue>[]
  data: TData[]
  filterColumnId: string 
  filterPlaceholder?: string
  initialSorting?: SortingState // 👈 Nova prop opcional
}

export function DataTable<TData, TValue>({
  columns,
  data,
  filterColumnId, 
  filterPlaceholder,
  initialSorting = [], // 👈 Define um array vazio como valor padrão se a prop não for passada
}: DataTableProps<TData, TValue>) {

  // 2. Usar 'initialSorting' para definir o estado inicial de 'sorting'.
  const [sorting, setSorting] = React.useState<SortingState>(initialSorting)
  const [rowSelection, setRowSelection] = React.useState({})
  const [columnFilters, setColumnFilters] = React.useState<ColumnFiltersState>([])

  const table = useReactTable({
    data,
    columns,
    // A configuração do hook já estava quase toda correta, apenas garantimos
    // que o estado inicial de 'sorting' vem da prop.
    onSortingChange: setSorting,
    getSortedRowModel: getSortedRowModel(),
    onColumnFiltersChange: setColumnFilters,
    getFilteredRowModel: getFilteredRowModel(),
    getCoreRowModel: getCoreRowModel(),
    getPaginationRowModel: getPaginationRowModel(),
    onRowSelectionChange: setRowSelection,
    state: {      
      sorting,
      rowSelection,
      columnFilters,
    },
  })

 return (
    <div className="w-full">
      <div className="flex items-center py-4">
        <Input
          placeholder={filterPlaceholder ?? `Filtrar por ${filterColumnId}...`}
          value={(table.getColumn(filterColumnId)?.getFilterValue() as string) ?? ""}
          onChange={(event) =>
              table.getColumn(filterColumnId)?.setFilterValue(event.target.value)
          }
          className="max-w-sm"
        />
      </div>      
      <div className="rounded-md sm:border">
          <Table>
              <TableHeader>
              {table.getHeaderGroups().map((headerGroup) => (
                  <TableRow key={headerGroup.id}>
                  {headerGroup.headers.map((header) => {
                      return (
                      <TableHead key={header.id} className="whitespace-nowrap">
                        {header.isPlaceholder ? null : (
                          // 3. Melhoria de UX: Renderiza um botão se a coluna for ordenável, senão, só texto.
                          header.column.getCanSort() ? (
                            <Button
                              variant="ghost"
                              onClick={header.column.getToggleSortingHandler()}
                              className="px-2 py-1"
                            >
                                {flexRender(header.column.columnDef.header, header.getContext())}
                                <ArrowUpDown className="ml-2 h-4 w-4" />
                            </Button>
                          ) : (
                            <div className="px-2 py-1">
                                {flexRender(header.column.columnDef.header, header.getContext())}
                            </div>
                          )
                        )}
                      </TableHead>
                      )
                  })}
                  </TableRow>
              ))}
              </TableHeader>
              <TableBody>
                {table.getRowModel().rows?.length ? (
                    table.getRowModel().rows.map((row) => (
                    <TableRow 
                        key={row.id}
                        data-state={row.getIsSelected() && "selected"}
                    >
                        {row.getVisibleCells().map((cell) => (
                        <TableCell key={cell.id}>
                            {flexRender(cell.column.columnDef.cell, cell.getContext())}
                        </TableCell>
                        ))}
                    </TableRow>
                    ))
                ) : (
                    <TableRow>
                    <TableCell colSpan={columns.length} className="h-24 text-center">
                        Nenhum resultado.
                    </TableCell>
                    </TableRow>
                )}
              </TableBody>
          </Table>
      </div>
      <div className="flex items-center justify-between py-4">
        <div className="flex-1 text-sm text-muted-foreground">
            {table.getFilteredSelectedRowModel().rows.length} de{" "}
            {table.getFilteredRowModel().rows.length} linha(s) selecionadas.
        </div>
        <div className="flex items-center space-x-2">
            <Button
                variant="outline"
                size="sm"
                onClick={() => table.previousPage()}
                disabled={!table.getCanPreviousPage()}
            >
            Anterior
            </Button>
            <Button
                variant="outline"
                size="sm"
                onClick={() => table.nextPage()}
                disabled={!table.getCanNextPage()}
            >
                Próximo
            </Button>
        </div>
      </div>
    </div>      
  ) 
}