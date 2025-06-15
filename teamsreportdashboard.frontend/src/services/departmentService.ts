import { CreateDepartmentDto, Department } from "@/types/Department";
import axiosConfig from "./axiosConfig";

export const getDepartments = async (): Promise<Department[]> => {
  const response = await axiosConfig.get('/departments');
  return response.data;
};

export const createDepartment = async (department: CreateDepartmentDto): Promise<Department> => {
  const response = await axiosConfig.post('/departments', department);
  return response.data;
};

export const updateDepartment = async (id: number, department: Partial<CreateDepartmentDto>): Promise<void> => {
  await axiosConfig.put(`/departments/${id}`, department);
};

export const deleteDepartment = async (id: number): Promise<void> => {
  await axiosConfig.delete(`/departments/${id}`);
};