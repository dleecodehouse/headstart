// core services
import { NgModule } from '@angular/core'
import { RouterModule, Routes } from '@angular/router'
import { PromotionTableComponent } from './components/promotion-table/promotion-table.component'
import { PromotionUsageTableComponent } from './components/promotion-usage-table/promotion-usage-table.component'

const routes: Routes = [
  { path: '', component: PromotionTableComponent },
  { path: 'promotionusage', component: PromotionUsageTableComponent },
  { path: 'new', component: PromotionTableComponent },
  { path: ':promotionID', component: PromotionTableComponent },
  { path: 'clone/:promotionID', component: PromotionTableComponent },
]
@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PromotionsRoutingModule {}
