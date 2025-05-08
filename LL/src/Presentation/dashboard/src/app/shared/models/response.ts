export interface ApiResponse<T> {
  isSuccess: boolean;
  data: T | null;
  errorMessage: string | null;
}
