import { Component, Input, OnInit } from '@angular/core'
import { HSOrder } from '@ordercloud/headstart-sdk'
import { ShopperContextService } from 'src/app/services/shopper-context/shopper-context.service'

@Component({
  templateUrl: './payment-list.component.html',
  styleUrls: ['./payment-list.component.scss'],
})
export class OCMPaymentList implements OnInit {
  @Input() payments: any
  @Input() order: HSOrder
  _userCurrency: string

  constructor(private context: ShopperContextService) {}

  ngOnInit(): void {
    this._userCurrency = this.context.currentUser.get().Currency
  }
}
