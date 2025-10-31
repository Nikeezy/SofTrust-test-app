export interface TopicDto {
  id: number;
  name: string;
}

export interface CreateMessageRequest {
  name: string;
  email: string;
  phone: string;
  topicId: number;
  text: string;
  recaptchaToken: string;
}

export interface MessageResponseDto {
  id: number;
  name: string;
  email: string;
  phone: string;
  topicId: number;
  topicName: string;
  text: string;
  createdAt: string;
}
