import { DatePipe, DecimalPipe } from "@angular/common";
import { Component, inject, signal, type OnInit } from "@angular/core";
import { FormBuilder, ReactiveFormsModule } from "@angular/forms";
import {
  Api,
  bookLoadBoardListing,
  searchRouteLoadBoard,
  type LoadBoardBookingRequest,
  type RouteLoadBoardListingDto,
} from "@logistics/shared/api";
import { Grid, Stack } from "@logistics/shared/components";
import { LocalizationService } from "@logistics/shared/services";
import { ButtonModule } from "primeng/button";
import { InputNumberModule } from "primeng/inputnumber";
import { InputTextModule } from "primeng/inputtext";
import { ProgressSpinnerModule } from "primeng/progressspinner";
import { TagModule } from "primeng/tag";
import { ToastService } from "@/core/services";
import { EmptyState, FormField, PageHeader } from "@/shared/components";
import { BookLoadDialog } from "../_components";
import { LoadBoardStore } from "../store";

@Component({
  selector: "app-loadboard-route-search",
  templateUrl: "./loadboard-route-search.html",
  imports: [
    BookLoadDialog,
    ButtonModule,
    DatePipe,
    DecimalPipe,
    EmptyState,
    FormField,
    Grid,
    InputNumberModule,
    InputTextModule,
    PageHeader,
    ProgressSpinnerModule,
    ReactiveFormsModule,
    Stack,
    TagModule,
  ],
})
export class LoadBoardRouteSearchComponent implements OnInit {
  private readonly api = inject(Api);
  private readonly toast = inject(ToastService);
  protected readonly store = inject(LoadBoardStore);
  protected readonly distanceUnitLabel =
    inject(LocalizationService, { optional: true })?.getDistanceUnitLabel() ?? "mi";

  protected readonly searching = signal(false);
  protected readonly booking = signal(false);
  protected readonly hasSearched = signal(false);
  protected readonly listings = signal<RouteLoadBoardListingDto[]>([]);
  protected readonly showBookDialog = signal(false);
  protected readonly selectedListing = signal<RouteLoadBoardListingDto | null>(null);

  protected readonly form = inject(FormBuilder).group({
    origin: [""],
    destination: [""],
    radius: [50],
  });

  ngOnInit(): void {
    void this.store.loadAll();
  }

  protected async search(): Promise<void> {
    const v = this.form.value;
    if (!v.origin?.trim() || !v.destination?.trim()) {
      this.toast.showError("Enter both origin and destination");
      return;
    }

    this.searching.set(true);
    try {
      const result = await this.api.invoke(searchRouteLoadBoard, {
        body: {
          origin: v.origin.trim(),
          destination: v.destination.trim(),
          radius: v.radius ?? 50,
          maxResults: 50,
        },
      });
      this.listings.set(result?.listings ?? []);
      this.hasSearched.set(true);
    } catch {
      this.toast.showError("Load search failed");
    } finally {
      this.searching.set(false);
    }
  }

  protected openBookDialog(listing: RouteLoadBoardListingDto): void {
    this.selectedListing.set(listing);
    this.showBookDialog.set(true);
  }

  protected async onBook(body: LoadBoardBookingRequest): Promise<void> {
    const listing = this.selectedListing();
    if (!listing?.listing?.id) {
      this.toast.showError("Load board listing is missing its internal booking ID");
      return;
    }

    this.booking.set(true);
    try {
      await this.api.invoke(bookLoadBoardListing, {
        listingId: listing.listing.id,
        body,
      });
      this.showBookDialog.set(false);
      this.toast.showSuccess("Load booked successfully! A new load has been created in your TMS.");
      this.listings.update((cur) =>
        cur.filter((l) => l.listing.externalListingId !== listing.listing.externalListingId),
      );
    } catch {
      this.toast.showError("Failed to book load");
    } finally {
      this.booking.set(false);
    }
  }

  protected formatRate(value: number | null | undefined): string {
    if (value == null) return "-";
    return `$${Math.round(value).toLocaleString()}`;
  }

  protected formatRatePerMile(value: number | null | undefined): string {
    if (value == null) return "-";
    return `$${value.toFixed(2)}/mi`;
  }
}
