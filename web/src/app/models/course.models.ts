export interface Course {
  id: string;
  userId: string;
  name: string;
  bookingUrl: string;
  platform: CoursePlatform;
  releaseSchedule: ReleaseSchedule;
  credentialsSaved: boolean;
  createdAt: Date;
  updatedAt: Date;
}

export interface CreateCourseRequest {
  name: string;
  bookingUrl: string;
  platform: CoursePlatform;
  releaseSchedule: ReleaseSchedule;
  credentials?: CourseCredential;
}

export interface CourseCredential {
  email: string;
  password: string;
}

export interface ReleaseSchedule {
  daysBeforeRelease: number;
  releaseTimeHour: number;
  releaseTimeMinute: number;
}

export enum CoursePlatform {
  CPS_GOLF = 'cps-golf',
  GOLF_NOW = 'golfnow',
  TEE_SNAP = 'teesnap',
  FORE_UP = 'foreup',
  OTHER = 'other'
}

export function getPlatformLabel(platform: CoursePlatform): string {
  const labels: Record<CoursePlatform, string> = {
    [CoursePlatform.CPS_GOLF]: 'CPS Golf',
    [CoursePlatform.GOLF_NOW]: 'GolfNow',
    [CoursePlatform.TEE_SNAP]: 'TeeSnap',
    [CoursePlatform.FORE_UP]: 'ForeUp',
    [CoursePlatform.OTHER]: 'Other'
  };
  return labels[platform];
}
