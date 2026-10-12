import { inject, Injectable } from '@angular/core';
import { ApiClientService } from '../../../core/http/services/api-client.service';
import { MasterValue } from '../models/master-value.model';
/** Feature API operations mapped to the existing ASP.NET Core controllers. */
@Injectable({ providedIn: 'root' })
export class MasterDataService {
  private readonly api = inject(ApiClientService);
  getPriorities() { return this.api.get<MasterValue[]>('api/master-data/ticket-priorities'); }
  getStatuses() { return this.api.get<MasterValue[]>('api/master-data/ticket-statuses'); }
}
