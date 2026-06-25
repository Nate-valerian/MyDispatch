import { Component, inject } from "@angular/core";
import { Router } from "@angular/router";
import { Grid, Icon, Stack, Typography } from "@logistics/shared/components";
import { ButtonModule } from "primeng/button";
import { CardModule } from "primeng/card";

interface QuickAction {
  label: string;
  description: string;
  icon: string;
  route: string;
  iconColor: "info" | "success" | "warning";
}

@Component({
  selector: "app-loadboard-quick-actions",
  templateUrl: "./loadboard-quick-actions.html",
  imports: [ButtonModule, CardModule, Grid, Icon, Stack, Typography],
})
export class LoadBoardQuickActions {
  protected readonly actions: readonly QuickAction[] = [
    {
      label: "AI Route Finder",
      description: "Find loads along a corridor with fit scores",
      icon: "route",
      route: "/loadboard/route-search",
      iconColor: "info",
    },
    {
      label: "Search Loads",
      description: "Filter by origin, destination, equipment",
      icon: "search",
      route: "/loadboard/search",
      iconColor: "info",
    },
    {
      label: "Post a Truck",
      description: "Advertise your available trucks",
      icon: "truck",
      route: "/loadboard/posted-trucks",
      iconColor: "success",
    },
    {
      label: "Configure Providers",
      description: "Connect to DAT, Truckstop, 123Loadboard",
      icon: "cog",
      route: "/loadboard/providers",
      iconColor: "warning",
    },
  ];

  private readonly router = inject(Router);

  protected navigate(route: string): void {
    this.router.navigateByUrl(route);
  }
}
