import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiService } from './api.service';
import {
  Course,
  CreateCourseRequest,
  CourseCredential
} from '../models/course.models';

@Injectable({
  providedIn: 'root'
})
export class CourseService extends ApiService {
  private coursesUrl = '/courses';

  constructor(http: HttpClient) {
    super(http);
  }

  getCourses(): Observable<Course[]> {
    return this.http.get<Course[]>(this.buildUrl(this.coursesUrl));
  }

  getCourse(id: string): Observable<Course> {
    return this.http.get<Course>(this.buildUrl(`${this.coursesUrl}/${id}`));
  }

  createCourse(request: CreateCourseRequest): Observable<Course> {
    return this.http.post<Course>(this.buildUrl(this.coursesUrl), request);
  }

  updateCourse(id: string, request: Partial<CreateCourseRequest>): Observable<Course> {
    return this.http.put<Course>(
      this.buildUrl(`${this.coursesUrl}/${id}`),
      request
    );
  }

  deleteCourse(id: string): Observable<void> {
    return this.http.delete<void>(this.buildUrl(`${this.coursesUrl}/${id}`));
  }

  saveCredentials(courseId: string, credentials: CourseCredential): Observable<void> {
    return this.http.post<void>(
      this.buildUrl(`${this.coursesUrl}/${courseId}/credentials`),
      credentials
    );
  }

  verifyCredentials(
    courseId: string,
    credentials: CourseCredential
  ): Observable<{ valid: boolean; message?: string }> {
    return this.http.post<{ valid: boolean; message?: string }>(
      this.buildUrl(`${this.coursesUrl}/${courseId}/verify-credentials`),
      credentials
    );
  }
}
