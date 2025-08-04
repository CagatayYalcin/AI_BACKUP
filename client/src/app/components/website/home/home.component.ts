import { Component, OnInit } from '@angular/core';
import { SubscriptionPlan } from '../../../models/subscription-plan.model';
import { SubscriptionService } from '../../../services/subscription.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss']
})
export class HomeComponent implements OnInit {
  subscriptionPlans: SubscriptionPlan[] = [];
  loading = false;
  error = '';

  constructor(private subscriptionService: SubscriptionService) { }

  ngOnInit(): void {
    this.loading = true;
    this.subscriptionService.getActiveSubscriptionPlans().subscribe({
      next: (plans) => {
        this.subscriptionPlans = plans;
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load subscription plans';
        this.loading = false;
        console.error(err);
      }
    });
  }
}