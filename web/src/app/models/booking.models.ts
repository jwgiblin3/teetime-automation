export interface BookingRequest {
  id: string;
  userId: string;
  courseId: string;
  courseName: string;
  requestedDate: Date;
  preferredTime: string;
  timeWindowMinutes: number;
  numberOfPlayers: number;
  status: BookingStatus;
  bookingResult?: BookingResult;
  createdAt: Date;
  updatedAt: Date;
  nextAttempt?: Date;
  attempts: number;
}

export interface CreateBookingRequest {
  courseId: string;
  requestedDate: Date;
  preferredTime: string;
  timeWindowMinutes: number;
  numberOfPlayers: number;
}

export interface BookingResult {
  confirmationNumber?: string;
  bookingTime?: string;
  bookingDate?: string;
  playersConfirmed: number;
  courseContact?: string;
  totalCost?: number;
  errorMessage?: string;
}

export enum BookingStatus {
  PENDING = 'pending',
  SCHEDULED = 'scheduled',
  IN_PROGRESS = 'in-progress',
  BOOKED = 'booked',
  FAILED = 'failed',
  CANCELLED = 'cancelled'
}

export function getStatusColor(status: BookingStatus): string {
  const colors: Record<BookingStatus, string> = {
    [BookingStatus.PENDING]: '#757575',
    [BookingStatus.SCHEDULED]: '#2196f3',
    [BookingStatus.IN_PROGRESS]: '#ff9800',
    [BookingStatus.BOOKED]: '#4caf50',
    [BookingStatus.FAILED]: '#d32f2f',
    [BookingStatus.CANCELLED]: '#616161'
  };
  return colors[status];
}

export function getStatusLabel(status: BookingStatus): string {
  const labels: Record<BookingStatus, string> = {
    [BookingStatus.PENDING]: 'Pending',
    [BookingStatus.SCHEDULED]: 'Scheduled',
    [BookingStatus.IN_PROGRESS]: 'In Progress',
    [BookingStatus.BOOKED]: 'Booked',
    [BookingStatus.FAILED]: 'Failed',
    [BookingStatus.CANCELLED]: 'Cancelled'
  };
  return labels[status];
}

export function getStatusIcon(status: BookingStatus): string {
  const icons: Record<BookingStatus, string> = {
    [BookingStatus.PENDING]: 'schedule',
    [BookingStatus.SCHEDULED]: 'alarm',
    [BookingStatus.IN_PROGRESS]: 'autorenew',
    [BookingStatus.BOOKED]: 'check_circle',
    [BookingStatus.FAILED]: 'cancel',
    [BookingStatus.CANCELLED]: 'block'
  };
  return icons[status];
}
