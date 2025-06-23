import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogDescription
} from "@/components/ui/dialog";
import { DataTable } from "@/components/CustomTable/DataTable";
import { ColumnDef } from "@tanstack/react-table";
import { ChartData } from "@/services/dashboardService";

// Props que o modal recebe
interface DashboardDetailsModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  data: ChartData[];
}

// Definição simples das colunas para a tabela de detalhes
const columns: ColumnDef<ChartData>[] = [
    {
      accessorKey: 'name',
      header: 'Item',
      enableSorting: false,
    },
    {
      accessorKey: 'total',
      header: () => <div className="text-center ">Total</div>,
      cell: ({ row }) => <div className="text-center font-medium">{row.getValue("total")}</div>,
    },
];

export const DashboardDetailsModal: React.FC<DashboardDetailsModalProps> = ({ isOpen, onClose, title, data }) => {
  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-md md:max-w-xl">
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>
            Lista completa de todos os itens e seus respectivos totais.
          </DialogDescription>
        </DialogHeader>
        <div className="mt-4 max-h-[60vh] overflow-y-auto">
          <DataTable
            columns={columns}
            data={data}
            filterColumnId="name"
            filterPlaceholder="Filtrar por nome..."
          />
        </div>
      </DialogContent>
    </Dialog>
  );
}