import { Component } from "@angular/core";
import { Avatar, SectionContainer, SectionHeader } from "@/shared/components";
import { ScrollAnimateDirective } from "@/shared/directives";

interface TeamMember {
  name: string;
  role: string;
  bio: string;
  initials: string;
  linkedIn?: string;
}

@Component({
  selector: "web-team",
  templateUrl: "./team.html",
  imports: [SectionContainer, SectionHeader, ScrollAnimateDirective, Avatar],
})
export class Team {
  protected readonly members: TeamMember[] = [
    {
      name: "Nate",
      role: "CEO & Founder",
      bio: "Software engineer and founder. Builds tools for the logistics industry.",
      initials: "N",
      linkedIn: "https://github.com/Nate-valerian",
    },
    {
      name: "Co-Founder",
      role: "Co-Founder",
      bio: "Details coming soon.",
      initials: "CF",
    },
  ];
}
