import { AfterViewInit, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgxMaskDirective } from 'ngx-mask';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { MessagesService } from '../../services/messages.service';
import { MessageResponseDto, TopicDto } from '../../models/api-models';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-contact-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgxMaskDirective],
  templateUrl: './contact-form.component.html',
  styleUrl: './contact-form.component.css'
})
export class ContactFormComponent implements OnInit, AfterViewInit, OnDestroy {
  private static recaptchaPromise: Promise<void> | null = null;

  private readonly formBuilder = inject(FormBuilder);
  private readonly messagesService = inject(MessagesService);
  private readonly destroy$ = new Subject<void>();

  readonly isRecaptchaConfigured =
    !!environment.recaptchaSiteKey && !environment.recaptchaSiteKey.includes('YOUR_RECAPTCHA_SITE_KEY');

  submitAttempted = false;
  recaptchaTouched = false;
  isSubmitting = false;
  serverError: string | null = null;
  topicsError: string | null = null;
  submittedMessage: MessageResponseDto | null = null;
  topics: TopicDto[] = [];

  private recaptchaWidgetId: number | null = null;

  readonly form = this.formBuilder.group({
    name: this.formBuilder.control('', [Validators.required, Validators.maxLength(200)]),
    email: this.formBuilder.control('', [
      Validators.required,
      Validators.pattern(/^[^@\s]+@[^@\s]+\.[^@\s]+$/)
    ]),
    phone: this.formBuilder.control('', [
      Validators.required,
      Validators.pattern(/^\+7 \(\d{3}\) \d{3}-\d{2}-\d{2}$/)
    ]),
    topicId: this.formBuilder.control<number | null>(null, [Validators.required]),
    text: this.formBuilder.control('', [Validators.required, Validators.maxLength(2000)]),
    recaptchaToken: this.formBuilder.control('', [Validators.required])
  });

  ngOnInit(): void {
    this.messagesService
      .getTopics()
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (items) => {
          if (items.length === 0) {
            this.topicsError = 'Темы сообщений пока не настроены.';
          } else {
            this.topics = items;
            this.topicsError = null;
          }
        },
        error: () => {
          this.topicsError = 'Не удалось загрузить список тем. Попробуйте позже.';
        }
      });
  }

  ngAfterViewInit(): void {
    if (!this.isRecaptchaConfigured) {
      console.warn('reCAPTCHA site key is not configured.');
      return;
    }

    this.loadRecaptchaScript()
      .then(() => this.renderRecaptcha())
      .catch((error) => {
        console.error('Failed to load reCAPTCHA script', error);
        this.topicsError = 'Не удалось инициализировать CAPTCHA.';
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  get controlsDisabled(): boolean {
    return this.isSubmitting;
  }

  get submitDisabled(): boolean {
    return this.isSubmitting || !this.isRecaptchaConfigured;
  }

  onSubmit(): void {
    this.submitAttempted = true;
    this.serverError = null;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.recaptchaTouched = true;
      return;
    }

    const value = this.form.getRawValue();
    const payload = {
      name: value.name!.trim(),
      email: value.email!.trim(),
      phone: value.phone!,
      topicId: Number(value.topicId),
      text: value.text!.trim(),
      recaptchaToken: value.recaptchaToken!
    };

    this.isSubmitting = true;

    this.messagesService
      .submitMessage(payload)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (response) => {
          this.submittedMessage = response;
          this.isSubmitting = false;
          this.form.reset();
          this.resetRecaptcha();
        },
        error: (error) => {
          console.error('Failed to submit message', error);
          this.serverError =
            error?.error?.error ?? 'Не удалось отправить сообщение. Попробуйте ещё раз.';
          this.isSubmitting = false;
          this.resetRecaptcha();
        }
      });
  }

  hasError(controlName: keyof typeof this.form.controls, error: string): boolean {
    const control = this.form.controls[controlName];
    if (!control) {
      return false;
    }
    return control.hasError(error) && (control.dirty || control.touched || this.submitAttempted);
  }

  trackByTopicId(_: number, topic: TopicDto): number {
    return topic.id;
  }

  onBack(): void {
    this.submittedMessage = null;
    this.serverError = null;
    this.form.reset();
    this.recaptchaTouched = false;
    this.resetRecaptcha();
    window.location.reload();
  }

  private resetRecaptcha(): void {
    const grecaptchaInstance = (window as any).grecaptcha as ReCaptchaV2.ReCaptcha | undefined;
    if (typeof window !== 'undefined' && grecaptchaInstance && this.recaptchaWidgetId !== null) {
      grecaptchaInstance.reset(this.recaptchaWidgetId);
      this.form.controls.recaptchaToken.setValue('');
    }
  }

  private loadRecaptchaScript(): Promise<void> {
    if ((window as any).grecaptcha?.render) {
      return Promise.resolve();
    }

    if (!ContactFormComponent.recaptchaPromise) {
      ContactFormComponent.recaptchaPromise = new Promise((resolve, reject) => {
        const script = document.createElement('script');
        script.src = 'https://www.google.com/recaptcha/api.js?render=explicit';
        script.async = true;
        script.defer = true;
        script.onload = () => resolve();
        script.onerror = (error) => reject(error);
        document.body.appendChild(script);
      });
    }

    return ContactFormComponent.recaptchaPromise;
  }

  private renderRecaptcha(): void {
    const grecaptchaInstance = (window as any).grecaptcha as ReCaptchaV2.ReCaptcha | undefined;
    if (!grecaptchaInstance) {
      console.error('grecaptcha is not available on the window object.');
      return;
    }

    grecaptchaInstance.ready(() => {
      const container = document.getElementById('recaptcha-container');
      if (!container) {
        console.error('recaptcha container not found');
        return;
      }

      this.recaptchaWidgetId = grecaptchaInstance.render(container, {
        sitekey: environment.recaptchaSiteKey,
        callback: (token: string) => {
          this.form.controls.recaptchaToken.setValue(token);
          this.recaptchaTouched = false;
        },
        'expired-callback': () => {
          this.form.controls.recaptchaToken.setValue('');
          this.recaptchaTouched = true;
        },
        'error-callback': () => {
          this.form.controls.recaptchaToken.setValue('');
          this.recaptchaTouched = true;
        }
      });
    });
  }
}
