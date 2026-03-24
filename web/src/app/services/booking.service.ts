import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  BookingRequest,
  CreateBookingRequest,
  BookingStatus
} from '../models/booking.models';

@Injectable({
  providedIn: 'root'
})
export class BookingService extends ApiService {
  private bookingsUrl = '/bookings';

  constructor(http: HttpClient) {
    super(http);
  }

  getBookings(status?: BookingStatus, courseId?: string): Observable<BookingRequest[]> {
    const params: Record<string, string | number | boolean> = {};
    if (status) {
      params['status'] = status;
    }
    if (courseId) {
      params['courseId'] = courseId;
    }

    const httpParams = this.buildParams(params);
    return this.http.get<BookingRequest[]>(
      this.buildUrl(this.bookingsUrl),
      { params: httpParams }
    );
  }

  getBooking(id: string): Observable<BookingRequest> {
    return this.http.get<BookingRequest>(
      this.buildUrl(`${this.bookingsUrl}/${id}`)
    );
  }

  createBooking(request: CreateBookingRequest): Observable<BookingRequest> {
    return this.http.post<BookingRequest>(
      this.buildUrl(this.bookingsUrl),
      request
    );
  }

  cancelBooking(id: string): Observable<void> {
    return this.http.post<void>(
      this.buildUrl(`${this.bookingsUrl}/${id}/cancel`),
      {}
    );
  }

  retryBooking(id: string): Observable<BookingRequest> {
    return this.http.post<BookingRequest>(
      this.buildUrl(`${this.bookingsUrl}/${id}/retry`),
      {}
    );
  }

  getBookingHistory(id: string): Observable<Array<{ timestamp: Date; status: BookingStatus; message: string }>> {
    return this.http.get<Array<{ timestamp: Date; status: BookingStatus; message: string }>>(
      this.buildUrl(`${this.bookingsUrl}/${id}/history`)
    );
  }
}
