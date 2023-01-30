import { Component, ChangeDetectorRef, NgZone } from '@angular/core'
import { PromotionUsage } from '../../models/promotion-usage'
import { ResourceCrudComponent } from '@app-seller/shared/components/resource-crud/resource-crud.component'
import { Router, ActivatedRoute } from '@angular/router'
import { PromotionService } from '../../promotion.service'

@Component({
  selector: 'app-promotion-usage-table',
  templateUrl: './promotion-usage-table.component.html',
  styleUrls: ['./promotion-usage-table.component.scss'],
})
export class PromotionUsageTableComponent extends ResourceCrudComponent<PromotionUsage> {
  constructor(
    private promotionService: PromotionService,
    changeDetectorRef: ChangeDetectorRef,
    router: Router,
    activatedRoute: ActivatedRoute,
    ngZone: NgZone
  ) {
    super(changeDetectorRef, promotionService, router, activatedRoute, ngZone)
  }

  filterConfig = {
    Filters: [
      {
        Display: 'ADMIN.FILTERS.APPLICATION',
        Path: 'xp.Automatic',
        Type: 'Dropdown',
        Items: [
          { Text: 'ADMIN.FILTER_OPTIONS.AUTOMATIC', Value: 'true' },
          { Text: 'ADMIN.FILTER_OPTIONS.CODE', Value: 'false' },
        ],
      },
      {
        Display: 'ADMIN.FILTERS.APPLIES_TO',
        Path: 'xp.AppliesTo',
        Type: 'Dropdown',
        Items: [
          { Text: 'ADMIN.FILTER_OPTIONS.ENTIRE_ORDER', Value: 'EntireOrder' },
          {
            Text: 'ADMIN.FILTER_OPTIONS.SPECIFIC_SUPPLIER',
            Value: 'SpecificSupplier',
          },
          { Text: 'ADMIN.FILTER_OPTIONS.SPECIFIC_SKUS', Value: 'SpecificSKUs' },
        ],
      },
    ],
  }
}
