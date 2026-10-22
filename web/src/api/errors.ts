import { ApiError } from './client';

/** Mensaje legible para el usuario a partir de cualquier error de una llamada a la API. */
export function messageOf(error: unknown): string {
  if (error instanceof ApiError) return error.message;
  return 'No se pudo contactar la API. Verifique que el contenedor "app" esté en ejecución.';
}

export function isConflict(error: unknown): error is ApiError {
  return error instanceof ApiError && error.status === 409;
}
