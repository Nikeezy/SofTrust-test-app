import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { CreateMessageRequest, MessageResponseDto, TopicDto } from '../models/api-models';
import { environment } from '../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class MessagesService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = environment.apiUrl.replace(/\/$/, '');

  getTopics(): Observable<TopicDto[]> {
    return this.http.get<TopicDto[]>(`${this.baseUrl}/api/topics`).pipe(
      map((topics) => topics.sort((a, b) => a.name.localeCompare(b.name)))
    );
  }

  submitMessage(payload: CreateMessageRequest): Observable<MessageResponseDto> {
    return this.http.post<MessageResponseDto>(`${this.baseUrl}/api/messages`, payload);
  }
}
