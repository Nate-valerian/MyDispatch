import { Component, computed, effect, inject, input, model, output, signal } from "@angular/core";
import { takeUntilDestroyed } from "@angular/core/rxjs-interop";
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from "@angular/forms";
import { regionAllowedCountries, UserRole } from "@logistics/shared";
import {
  Api,
  getEmployees,
  updateEmployee,
  type Address,
  type EmployeeDto,
  type EmployeeStatus,
  type SalaryType,
  type UpdateEmployeeCommand,
} from "@logistics/shared/api";
import { employeeStatusOptions, salaryTypeOptions } from "@logistics/shared/api/enums";
import { AddressForm, Stack } from "@logistics/shared/components";
import { AccordionModule } from "primeng/accordion";
import { ButtonModule } from "primeng/button";
import { DialogModule } from "primeng/dialog";
import { InputGroupModule } from "primeng/inputgroup";
import { InputTextModule } from "primeng/inputtext";
import { SelectModule } from "primeng/select";
import { AuthService } from "@/core/auth";
import { TenantService } from "@/core/services/tenant.service";
import { CurrencyInput, FormField, UnitInput, ValidationSummary } from "@/shared/components";
import { NumberUtils } from "@/shared/utils";
import { ChangeRoleDialog } from "../change-role-dialog/change-role-dialog";

@Component({
  selector: "app-employee-edit-dialog",
  templateUrl: "./employee-edit-dialog.html",
  imports: [
    DialogModule,
    ButtonModule,
    ReactiveFormsModule,
    SelectModule,
    InputGroupModule,
    InputTextModule,
    AccordionModule,
    FormField,
    UnitInput,
    CurrencyInput,
    ValidationSummary,
    AddressForm,
    ChangeRoleDialog,
    Stack,
  ],
})
export class EmployeeEditDialog {
  private readonly api = inject(Api);
  private readonly authService = inject(AuthService);
  private readonly tenantService = inject(TenantService);

  protected readonly allowedCountries = computed(() =>
    regionAllowedCountries(this.tenantService.tenantData()?.settings?.region),
  );

  readonly visible = model<boolean>(false);
  readonly employee = input<EmployeeDto | null>(null);
  readonly saved = output<void>();
  readonly deleted = output<void>();

  protected readonly form: FormGroup<UpdateEmployeeForm>;
  protected readonly salaryTypes = salaryTypeOptions;
  protected readonly statusOptions = employeeStatusOptions;
  protected readonly isLoading = signal(false);
  protected readonly canChangeRole = signal(false);
  protected readonly changeRoleDialogVisible = signal(false);
  protected readonly isDriver = computed(() => this.employee()?.role?.name === UserRole.Driver);
  protected readonly dispatchers = signal<{ label: string; value: string | null }[]>([]);

  constructor() {
    this.form = new FormGroup<UpdateEmployeeForm>({
      salary: new FormControl<number>(0, {
        validators: Validators.compose([Validators.required, Validators.min(0)]),
        nonNullable: true,
      }),
      salaryType: new FormControl<SalaryType>("none", {
        validators: Validators.required,
        nonNullable: true,
      }),
      status: new FormControl<EmployeeStatus>("active", {
        validators: Validators.required,
        nonNullable: true,
      }),
      address: new FormControl<Address | null>(null),
      assignedDispatcherId: new FormControl<string | null>(null),
    });

    this.form
      .get("salaryType")
      ?.valueChanges.pipe(takeUntilDestroyed())
      .subscribe((selectedSalaryType) => {
        const salaryControl = this.form.get("salary");
        if (!salaryControl) return;

        if (selectedSalaryType === "share_of_gross") {
          salaryControl.setValidators([
            Validators.required,
            Validators.min(0),
            Validators.max(100),
          ]);
          salaryControl.enable();
        } else if (selectedSalaryType === "none") {
          salaryControl.setValue(0);
          salaryControl.disable();
        } else {
          salaryControl.setValidators([Validators.required, Validators.min(0)]);
          salaryControl.enable();
        }
        salaryControl.updateValueAndValidity();
      });

    effect(() => {
      const emp = this.employee();
      if (emp && this.visible()) {
        this.populateForm(emp);
        this.evaluateCanChangeRole(emp);
        if (emp.role?.name === UserRole.Driver) {
          this.loadDispatchers();
        }
      }
    });
  }

  async save(): Promise<void> {
    if (!this.form.valid) return;

    const emp = this.employee();
    if (!emp?.id) return;

    const salaryType = this.form.value.salaryType!;
    const salary = this.form.value.salary!;
    const status = this.form.value.status!;

    const isDriver = emp.role?.name === UserRole.Driver;
    const command: UpdateEmployeeCommand = {
      userId: emp.id,
      salary: salaryType === "share_of_gross" ? NumberUtils.toRatio(salary) : salary,
      salaryType: salaryType,
      status: status,
      address: this.form.value.address ?? undefined,
      updateAssignedDispatcher: isDriver,
      assignedDispatcherId: isDriver ? (this.form.value.assignedDispatcherId ?? null) : undefined,
    };

    this.isLoading.set(true);
    try {
      await this.api.invoke(updateEmployee, {
        userId: emp.id,
        body: command,
      });
      this.saved.emit();
    } finally {
      this.isLoading.set(false);
    }
  }

  close(): void {
    this.visible.set(false);
  }

  openChangeRoleDialog(): void {
    this.changeRoleDialogVisible.set(true);
  }

  onRoleChanged(): void {
    this.changeRoleDialogVisible.set(false);
    this.saved.emit();
  }

  isShareOfGrossSalary(): boolean {
    return this.form.value.salaryType === "share_of_gross";
  }

  isNoneSalary(): boolean {
    return this.form.value.salaryType === "none";
  }

  private populateForm(emp: EmployeeDto): void {
    const salaryType = emp.salaryType ?? "none";
    const salary = emp.salary ?? 0;

    this.form.patchValue({
      salary: salaryType === "share_of_gross" ? NumberUtils.toPercent(salary) : salary,
      salaryType: salaryType,
      status: emp.status ?? "active",
      address: emp.address ?? null,
      assignedDispatcherId: emp.assignedDispatcherId ?? null,
    });
  }

  private async loadDispatchers(): Promise<void> {
    try {
      const result = await this.api.invoke(getEmployees, { Role: "Dispatcher", PageSize: 100 });
      const options = (result?.items ?? []).map((d) => ({
        label: d.fullName ?? d.email ?? d.id ?? "",
        value: d.id ?? null,
      }));
      this.dispatchers.set([{ label: "None", value: null }, ...options]);
    } catch {
      this.dispatchers.set([{ label: "None", value: null }]);
    }
  }

  private evaluateCanChangeRole(emp: EmployeeDto): void {
    const user = this.authService.getUserData();
    const userRole = user?.role;
    const employeeRole = emp.role?.name;

    if (!userRole) {
      this.canChangeRole.set(false);
      return;
    }

    if (!employeeRole) {
      this.canChangeRole.set(true);
      return;
    }

    if (userRole === UserRole.AppSuperAdmin || userRole === UserRole.AppAdmin) {
      this.canChangeRole.set(true);
    } else if (userRole === UserRole.Owner && employeeRole !== UserRole.Owner) {
      this.canChangeRole.set(true);
    } else if (
      userRole === UserRole.Manager &&
      employeeRole !== UserRole.Owner &&
      employeeRole !== UserRole.Manager
    ) {
      this.canChangeRole.set(true);
    } else {
      this.canChangeRole.set(false);
    }
  }
}

interface UpdateEmployeeForm {
  salary: FormControl<number>;
  salaryType: FormControl<SalaryType>;
  status: FormControl<EmployeeStatus>;
  address: FormControl<Address | null>;
  assignedDispatcherId: FormControl<string | null>;
}
