// src/pages/Users/UsersPage.tsx
import React, { useEffect, useState } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '../../components/CustomTable/DataTable';
import { MoreHorizontal } from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
// useNavigate não é mais necessário para redirecionamento de login aqui
import { EditUserModal } from '@/components/EditUserModal'; // Assumindo que este componente existe e funciona
import { getUsers, deleteUser } from '@/services/userService'; // Seus serviços
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext'; // 👈 Importe useAuth
import { User } from '@/types/User';
import { RoleEnum } from '@/utils/role'; // Assumindo que você tem um arquivo de utilitário para roles

// A interface User aqui é para a lista de usuários, pode ser diferente do User do AuthContext
// Se for a mesma, você pode importar de src/types/User.ts

const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]);
  const { user: currentUser, isLoading: authIsLoading } = useAuth(); // 👈 Obtenha o usuário logado do contexto
  
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [dataLoading, setDataLoading] = useState(true); // Estado de loading para os dados da tabela

  useEffect(() => {
    // MasterRoute já garante que currentUser é um Master ou redireciona.
    // Se currentUser não estiver carregado (authIsLoading), esperamos.
    if (authIsLoading) {
      return; // Aguarda o AuthContext carregar o usuário
    }

    // Se, por alguma razão extraordinária, currentUser for null aqui após authIsLoading ser false,
    // e MasterRoute não redirecionou, é um estado inesperado.
    // Mas MasterRoute deve ter cuidado disso.
    
    const fetchUsers = async () => {
      setDataLoading(true);
      try {
        const data = await getUsers(); // Seu serviço para buscar todos os usuários
        setUsers(data);
      } catch (error) {
        console.error('Erro ao buscar usuários:', error);
        toast.error("Erro ao buscar usuários. Verifique o console para detalhes.");
      } finally {
        setDataLoading(false);
      }
    };

    fetchUsers();
  }, [authIsLoading]); // Depende de authIsLoading para garantir que currentUser foi processado

  const handleDelete = async (id: number) => {
    if (!currentUser) return; // Guarda de segurança

    if (id === currentUser.id) {
      toast.warning("Você não pode excluir a si mesmo.");
      return;
    }

    const confirmed = window.confirm("Você tem certeza que deseja excluir este usuário?");
    if (!confirmed) return;

    try {
      await deleteUser(id);
      setUsers(prev => prev.filter(user => user.id !== id));
      toast.success("O usuário foi removido com sucesso.");
    } catch (error: any) {
      const message = error?.response?.data?.message || "Erro ao excluir o usuário.";
      toast.error("Erro ao excluir usuário: " + message);
      console.error("Erro ao excluir usuário:", error);
    }
  };

  const handleEdit = (user: User) => {
    setEditingUser(user);
    setIsModalOpen(true);
  };

  // Chamado pelo EditUserModal quando um usuário é salvo com sucesso
  const handleUserUpdate = (updatedUser: User) => {
    setUsers(prev =>
      prev.map(user => (user.id === updatedUser.id ? updatedUser : user))
    );
    // Opcionalmente, pode recarregar todos os usuários se a atualização for complexa
    // fetchUsers();
  };

  const columns: ColumnDef<User>[] = [
    {
      id: "select",
      header: ({ table }) => ( <Checkbox checked={table.getIsAllPageRowsSelected() || (table.getIsSomePageRowsSelected() && "indeterminate")} onCheckedChange={(value) => table.toggleAllPageRowsSelected(!!value)} aria-label="Select all" /> ),
      cell: ({ row }) => ( <Checkbox checked={row.getIsSelected()} onCheckedChange={(value) => row.toggleSelected(!!value)} aria-label="Select row" /> ),
      enableSorting: false,
      enableHiding: false,
    },
    { accessorKey: 'name', header: 'Nome' },
    { accessorKey: 'email', header: 'Email' },
    {
      accessorKey: 'role',
      header: 'Função',
      cell: ({ row }) => {
        // Este roleMap assume que user.role na lista de usuários é uma string "0", "1", "2"
        // Se getUsers() retornar role como número, ajuste o acesso: roleMap[String(row.getValue('role'))]
        const roleMap: Record<string, string> = {
          "0": "Master",
          "1": "Admin",
          "2": "Viewer"
        };
        const roleValue = row.getValue('role') as string;
        return <div>{roleMap[roleValue] || "Desconhecido"}</div>;
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => <div>{row.getValue('isActive') ? 'Ativo' : 'Inativo'}</div>,
    },
    {
      id: "actions",
      cell: ({ row }) => {
        const userRowData = row.original;
        return (
          <DropdownMenu>
            <DropdownMenuTrigger asChild>
              <Button variant="ghost" className="h-8 w-8 p-0"><span className="sr-only">Open menu</span><MoreHorizontal className="h-4 w-4" /></Button>
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
              <DropdownMenuLabel>Ações</DropdownMenuLabel>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => handleEdit(userRowData)}>Editar</DropdownMenuItem>
              <DropdownMenuItem 
                onClick={() => handleDelete(userRowData.id)}
                disabled={currentUser?.id === userRowData.id} // Desabilita se for o próprio usuário
                className={currentUser?.id === userRowData.id ? "text-muted-foreground" : ""}
              >
                Excluir
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        );
      }
    },
  ];

  if (authIsLoading || dataLoading) { // Mostra loading enquanto o user do contexto ou os dados da tabela carregam
    return <div className="container mx-auto py-10 text-center">Carregando dados...</div>;
  }

  // Se MasterRoute falhou em redirecionar e currentUser não é Master por algum motivo (não deveria acontecer)
  if (!currentUser || currentUser.role !== RoleEnum.Master) {
      return <div className="container mx-auto py-10 text-center">Acesso não autorizado.</div>;
  }


  return (
    <div className='container mx-auto py-10'>
      <h1 className="text-2xl font-bold mb-6">Gerenciamento de Usuários</h1>
      <DataTable
        columns={columns}
        data={users}
      />
      {editingUser && ( // Garante que editingUser não é null antes de renderizar o modal
        <EditUserModal
          user={editingUser}
          isOpen={isModalOpen}
          onClose={() => {
            setIsModalOpen(false);
            setEditingUser(null);
          }}
          onSave={handleUserUpdate} // Esta função atualiza o estado local 'users'
        />
      )}
    </div>
  );
};

export default UsersPage;