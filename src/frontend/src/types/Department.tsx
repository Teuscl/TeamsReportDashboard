export interface Department {
  id: number;
  name: string;
  createdAt: string; // A data virá como string do JSON
}

// O tipo para criar um novo departamento não precisa de id ou createdAt
export type CreateDepartmentDto = Omit<Department, 'id' | 'createdAt'>;