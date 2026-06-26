export interface ArenaTicketStatus {
  currentTickets: number;
  lastTicketUpdate: Date;
  maxTickets: number;
  nextTicketAt?: Date | null;
}
