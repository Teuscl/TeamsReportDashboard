import DataTable from 'datatables.net-react';
// @ts-ignore
import language from 'datatables.net-plugins/i18n/pt-BR.mjs';
import DT from 'datatables.net-bs5';
import { Button, ButtonGroup } from 'react-bootstrap';
import { MdDelete, MdEditSquare } from "react-icons/md";
import { JSX, useEffect } from 'react';

DataTable.use(DT);

interface CustomTableProps<T> {
    data: T[];
    columns: { 
        title: string; 
        data?: string; 
        render?: (value: any, row: T) => JSX.Element | string; // Permitir dois argumentos no render
        orderable?: boolean;
    }[];
    actions?: {
        onEdit?: (row: T) => void;
        onDelete?: (row: T) => void;
        onExtra?: (row: T) => void;
    };
    extraAction?: any;
    options?: any; // Adicionado para permitir passar configurações ao DataTables
}

export const CustomTable = <T,>({ data, columns, actions, extraAction, options }: CustomTableProps<T>) => {
    useEffect(() => {
        // Modifica os cabeçalhos depois que a tabela é montada
        setTimeout(() => {
            document.querySelectorAll('th').forEach((th) => {
                const columnText = th.textContent?.trim() || "";

                // Se não for a coluna "Ações", adiciona o ícone de ordenação
                if (columnText !== "Ações" && !th.classList.contains('custom-header')) {
                    th.classList.add('custom-header');
                    th.innerHTML += ' <i class="fas fa-sort"></i>'; // Adiciona ícone de ordenação (FontAwesome)
                }
            });
        }, 500);
    }, [data]);

    // Atualiza colunas para impedir ordenação da coluna "Ações"
    const updatedColumns = columns.map(col => ({
        ...col,
        orderable: col.title === "Ações" ? false : col.orderable ?? true, // Desativa ordenação para "Ações"
    }));

    return (
        <DataTable
            options={{
                language,
                paging: true,
                searching: true,
                ordering: true,
                ...options,
            }}
            className="table table-hover table-responsive"
            columns={updatedColumns}
            data={data}
            slots={{
                'actions': (_data: any, row: T) => (
                    <ButtonGroup>
                        {actions?.onEdit && (
                            <Button variant="warning" size="sm" onClick={() => actions.onEdit?.(row)}>
                                <MdEditSquare />
                            </Button>
                        )}
                        {actions?.onExtra && (
                            <Button variant="info" size="sm" onClick={() => actions.onExtra?.(row)}>
                                {extraAction}
                            </Button>
                        )}
                        {actions?.onDelete && (
                            <Button variant="danger" size="sm" onClick={() => actions.onDelete?.(row)}>
                                <MdDelete />
                            </Button>
                        )}
                    </ButtonGroup>
                ),
            }}
        />
    );
};
