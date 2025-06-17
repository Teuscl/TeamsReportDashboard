// DTO para visualizar dados (vem da API com nome do depto)
export interface RequesterDto {
  id: number;
  name: string;
  email: string;
  departmentId?: number | null;
  departmentName?: string | null;
}

// DTO para criar um novo solicitante
export interface CreateRequesterDto {
  name: string;
  email: string;
  departmentId?: number | null;
}

// DTO para atualizar um solicitante
export interface UpdateRequesterDto extends CreateRequesterDto {}