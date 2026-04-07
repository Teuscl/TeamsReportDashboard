export enum RoleEnum {
  Master = 0,
  Admin = 1,
  Viewer = 2,
}

// Label para exibição
const roleLabels: Record<RoleEnum, string> = {
  [RoleEnum.Master]: "Master",
  [RoleEnum.Admin]: "Admin",
  [RoleEnum.Viewer]: "Viewer",
};

// Reverso para envio
const roleValues: Record<string, RoleEnum> = {
  Master: RoleEnum.Master,
  Admin: RoleEnum.Admin,
  Viewer: RoleEnum.Viewer,
};

/**
 * Obtém o nome da função baseado no valor da enum.
 */
export const getRoleLabel = (value: string | number): string => {
  const role = typeof value === "string" ? Number(value) : value;
  return roleLabels[role as RoleEnum] ?? "Desconhecido";
};

/**
 * Obtém o valor numérico da enum baseado no nome da função.
 */
export const getRoleValue = (label: string): RoleEnum => {
  return roleValues[label] ?? -1;
};
