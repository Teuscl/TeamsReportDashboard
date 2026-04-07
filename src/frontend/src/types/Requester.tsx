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

export interface BulkInsertFailure {
  rowNumber: number;
  errorMessage: string;
  offendingLine: string;
}

export interface BulkInsertResultDto {
  successfulInserts: number;
  failures: BulkInsertFailure[];
  hasErrors: boolean;
}

// DTO para atualizar um solicitante
export interface UpdateRequesterDto extends CreateRequesterDto {}