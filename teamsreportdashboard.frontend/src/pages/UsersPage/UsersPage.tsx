// src/pages/Users/UsersPage.tsx
import React, { useEffect, useState, useCallback } from 'react';
import { ColumnDef } from '@tanstack/react-table';
import { DataTable } from '../../components/CustomTable/DataTable';
import { MoreHorizontal } from "lucide-react"; // Ícone User de lucide-react não estava sendo usado, removi
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
import { UserFormModal } from '@/components/UserFormModal'; // 👈 Importando o modal unificado
import { getUsers, deleteUser } from '@/services/userService';
import { toast } from 'sonner';
import { useAuth } from '@/context/AuthContext';
import { User } from '@/types/User'; // 👈 Sua interface User global de @/types/User
import { RoleEnum, getRoleLabel } from '@/utils/role'; // 👈 Seus utilitários de Role
import { ArrowUpDown } from "lucide-react"
import AdminResetPasswordModal from '@/components/AdminResetPasswordModal'; // 👈 Importe o novo modal


const UsersPage: React.FC = () => {
  const [users, setUsers] = useState<User[]>([]); // Usando a interface User global
  const { user: currentUser, isLoading: authIsLoading } = useAuth();
  
  const [modalMode, setModalMode] = useState<'create' | 'edit' | null>(null);
  const [userForModal, setUserForModal] = useState<User | null>(null); // Usuário para edição ou nulo para criação
  const [dataLoading, setDataLoading] = useState(true);

  const [userToResetPassword, setUserToResetPassword] = useState<User | null>(null); // 👈 Estado para o usuário do reset
  const [isResetPasswordModalOpen, setIsResetPasswordModalOpen] = useState(false);    // 👈 Estado para visibilidade do modal de reset

  const fetchUsers = useCallback(async () => {
    setDataLoading(true);
    try {
      const data = await getUsers(); // getUsers deve retornar User[] com role: RoleEnum (numérico)
      setUsers(data);
    } catch (error) {
      console.error('Erro ao buscar usuários:', error);
      toast.error("Erro ao buscar usuários. Verifique o console para detalhes.");
    } finally {
      setDataLoading(false);
    }
  }, []);

  useEffect(() => {
    if (authIsLoading) {
      return; 
    }
    // MasterRoute já garante que currentUser é Master, então podemos buscar os usuários.
    if (currentUser && currentUser.role === RoleEnum.Master) {
        fetchUsers();
    }
  }, [authIsLoading, currentUser, fetchUsers]); // Adicionado currentUser e fetchUsers como dependências

  const handleDelete = async (id: number) => {
    if (!currentUser) return;
    if (id === currentUser.id) {
      toast.warning("Você não pode excluir a si mesmo.");
      return;
    }
    const confirmed = window.confirm("Você tem certeza que deseja excluir este usuário?");
    if (!confirmed) return;

    try {
      await deleteUser(id);
      // Atualiza o estado local removendo o usuário ou recarrega a lista
      setUsers(prev => prev.filter(user => user.id !== id));
      toast.success("O usuário foi removido com sucesso.");
    } catch (error: any) {
      const message = error?.response?.data?.message || "Erro ao excluir o usuário.";
      toast.error(`Erro ao excluir usuário: ${message}`);
    }
  };

  const handleOpenCreateModal = () => {
    setUserForModal(null); // Nenhum usuário para editar, é criação
    setModalMode('create');
  };

  const handleOpenEditModal = (userToEdit: User) => {
    setUserForModal(userToEdit);
    setModalMode('edit');
  };
   const handleOpenResetPasswordModal = (userToReset: User) => { // 👈 Nova função
    setUserToResetPassword(userToReset);
    setIsResetPasswordModalOpen(true);
  };

  const closeResetPasswordModal = () => { // 👈 Nova função
    setIsResetPasswordModalOpen(false);
    setUserToResetPassword(null);
  };

  const handlePasswordResetSuccess = () => { // 👈 Nova função
    // A senha foi alterada, não há necessidade de recarregar a lista de usuários por isso.
    // Apenas fechamos o modal. O toast de sucesso já é mostrado dentro do modal.
    closeResetPasswordModal();
  };


  const closeModal = () => {
    setModalMode(null);
    setUserForModal(null);
  };

  // Chamado após sucesso na criação ou edição dentro do UserFormModal
  const handleSaveSuccess = () => {
    toast.info("Atualizando lista de usuários..."); // Feedback opcional
    fetchUsers(); // Recarrega a lista de usuários
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
        // Assumindo que row.original.role é do tipo RoleEnum (numérico)
        // como definido na interface User global e retornado por getUsers()
        return <div>{getRoleLabel(row.original.role)}</div>; // 👈 Usando getRoleLabel
      },
    },
    {
      accessorKey: 'isActive',
      header: 'Status',
      cell: ({ row }) => <div>{row.original.isActive ? 'Ativo' : 'Inativo'}</div>,
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
              <DropdownMenuItem onClick={() => handleOpenEditModal(userRowData)}>Editar</DropdownMenuItem>
              <DropdownMenuItem 
                onClick={() => handleDelete(userRowData.id)}
                disabled={currentUser?.id === userRowData.id}
                className={currentUser?.id === userRowData.id ? "text-muted-foreground cursor-not-allowed" : "cursor-pointer"}
              >
                Excluir
              </DropdownMenuItem>
              <DropdownMenuSeparator />
              <DropdownMenuItem onClick={() => handleOpenResetPasswordModal(userRowData)}> 
                Resetar Senha
              </DropdownMenuItem>
            </DropdownMenuContent>
          </DropdownMenu>
        );
      }
    },
  ];

  if (authIsLoading) { // Prioridade para o carregamento da autenticação
    return <div className="container mx-auto py-10 text-center">Carregando informações de autenticação...</div>;
  }

  // MasterRoute já deve ter redirecionado se não for Master.
  // Esta é uma verificação adicional de segurança ou para o caso de carregamento inicial.
  if (!currentUser || currentUser.role !== RoleEnum.Master) {
      return <div className="container mx-auto py-10 text-center">Acesso não autorizado.</div>;
  }
  
  // Se currentUser é Master, mas os dados da tabela ainda estão carregando
  if (dataLoading && users.length === 0) { 
      return <div className="container mx-auto py-10 text-center">Carregando usuários...</div>;
  }

  return (
    <div className='container mx-auto py-10 px-4 md:px-0'>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-2xl md:text-3xl font-bold">Gerenciamento de Usuários</h1>
        <Button onClick={handleOpenCreateModal}> {/* 👈 Botão para abrir o modal de criação */}
          Criar Usuário
        </Button>
      </div>
      <DataTable
        columns={columns}
        data={users}
        filterColumnId="email"
        filterPlaceholder="Filtrar por email do usuário..."        
      />
      {/* Renderiza o UserFormModal se modalMode estiver definido */}
      {modalMode && (
        <UserFormModal
          mode={modalMode}
          userToEdit={userForModal}
          isOpen={!!modalMode}
          onClose={closeModal}
          onSaveSuccess={handleSaveSuccess}
        />
      )}

      <AdminResetPasswordModal
          userToReset={userToResetPassword}
          isOpen={isResetPasswordModalOpen}
          onClose={closeResetPasswordModal}
          onSuccess={handlePasswordResetSuccess}
      />
    </div>
  );
};

export default UsersPage;