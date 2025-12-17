using System;
using System.Collections.Generic;

namespace HospitalMgmt
{
    // Event args to carry patient info
    public class PatientEventArgs : EventArgs
    {
        public Patient Patient { get; }
        public decimal Amount { get; }

        public PatientEventArgs(Patient p, decimal amount = 0)
        {
            Patient = p;
            Amount = amount;
        }
    }

    // Delegate for billing strategy
    public delegate decimal BillingStrategy(Patient p);

    // Base patient class
    public abstract class Patient
    {
        public string Id { get; }
        public string Name { get; set; }
        public string Email { get; set; }

        protected Patient(string id)
        {
            Id = id;
        }

        public abstract string Type { get; }
    }

    public class InPatient : Patient
    {
        public int DaysAdmitted { get; set; }
        public decimal RoomRate { get; set; }

        public InPatient(string id) : base(id) { }
        public override string Type => "InPatient";
    }

    public class OutPatient : Patient
    {
        public OutPatient(string id) : base(id) { }
        public override string Type => "OutPatient";
    }

    public class EmergencyPatient : Patient
    {
        public bool Critical { get; set; }
        public EmergencyPatient(string id) : base(id) { }
        public override string Type => "Emergency";
    }

    // Central manager that raises events and applies billing delegates
    public class PatientManager
    {
        // Events
        public event EventHandler<PatientEventArgs> PatientAdmitted;
        public event EventHandler<PatientEventArgs> BillGenerated;

        // Departments subscribe to these events

        // Apply billing strategy (delegate)
        public decimal ApplyBillingStrategy(Patient p, BillingStrategy strategy)
        {
            if (strategy == null) throw new ArgumentNullException(nameof(strategy));
            var amount = strategy(p);
            OnBillGenerated(new PatientEventArgs(p, amount));
            return amount;
        }

        // Admit patient and trigger event
        public void AdmitPatient(Patient p)
        {
            OnPatientAdmitted(new PatientEventArgs(p));
        }

        protected virtual void OnPatientAdmitted(PatientEventArgs e)
        {
            PatientAdmitted?.Invoke(this, e);
        }

        protected virtual void OnBillGenerated(PatientEventArgs e)
        {
            BillGenerated?.Invoke(this, e);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var mgr = new PatientManager();

            // Subscribe departments to events
            mgr.PatientAdmitted += (s, e) => Console.WriteLine($"[NOTIFY] Admissions: Patient {e.Patient.Name} ({e.Patient.Type}) admitted.");
            mgr.PatientAdmitted += (s, e) => Console.WriteLine($"[NOTIFY] Nursing: Prepare bed and chart for {e.Patient.Name}.");
            mgr.BillGenerated += (s, e) => Console.WriteLine($"[NOTIFY] Billing: Bill for {e.Patient.Name} generated. Amount: {e.Amount:C}");
            mgr.BillGenerated += (s, e) => Console.WriteLine($"[NOTIFY] Pharmacy: Prepare medications for {e.Patient.Name} if applicable.");

            Console.WriteLine("Welcome to the Hospital Patient Management Console");

            while (true)
            {
                Console.WriteLine();
                Console.WriteLine("1) Admit Patient");
                Console.WriteLine("2) Exit");
                Console.Write("Select option: ");
                var opt = Console.ReadLine();
                if (opt == "2") break;

                if (opt == "1")
                {
                    var id = Guid.NewGuid().ToString().Split('-')[0];
                    Console.Write("Name: ");
                    var name = Console.ReadLine();
                    Console.Write("Email: ");
                    var email = Console.ReadLine();

                    Console.WriteLine("Select patient type: 1-InPatient 2-OutPatient 3-Emergency");
                    var t = Console.ReadLine();
                    Patient p;
                    if (t == "1")
                    {
                        var ip = new InPatient(id) { Name = name, Email = email };
                        Console.Write("Days admitted: ");
                        if (int.TryParse(Console.ReadLine(), out var days)) ip.DaysAdmitted = days; else ip.DaysAdmitted = 1;
                        Console.Write("Room rate per day: ");
                        if (decimal.TryParse(Console.ReadLine(), out var rr)) ip.RoomRate = rr; else ip.RoomRate = 1000m;
                        p = ip;
                    }
                    else if (t == "3")
                    {
                        var ep = new EmergencyPatient(id) { Name = name, Email = email };
                        Console.Write("Is critical? (y/n): ");
                        ep.Critical = (Console.ReadLine() ?? string.Empty).Trim().ToLower().StartsWith("y");
                        p = ep;
                    }
                    else
                    {
                        p = new OutPatient(id) { Name = name, Email = email };
                    }

                    // Admit and notify
                    mgr.AdmitPatient(p);

                    // Choose billing strategy dynamically
                    BillingStrategy strategy = ChooseBillingStrategy(p);
                    var amount = mgr.ApplyBillingStrategy(p, strategy);

                    // Generate and display bill
                    Console.WriteLine("---- BILL ----");
                    Console.WriteLine($"Patient: {p.Name} ({p.Type})");
                    Console.WriteLine($"Amount Due: {amount:C}");
                    Console.WriteLine("---------------");
                }
            }

            Console.WriteLine("Exiting. Goodbye.");
        }

        static BillingStrategy ChooseBillingStrategy(Patient p)
        {
            // For demonstration we return different strategies based on runtime type
            if (p is InPatient ip)
            {
                return (patient) =>
                {
                    var inp = (InPatient)patient;
                    decimal baseCharge = 500m; // treatment base
                    decimal room = inp.DaysAdmitted * inp.RoomRate;
                    decimal treatment = baseCharge + (inp.DaysAdmitted * 200);
                    return room + treatment;
                };
            }

            if (p is EmergencyPatient ep)
            {
                return (patient) =>
                {
                    var ept = (EmergencyPatient)patient;
                    decimal consult = 800m;
                    decimal criticalSurcharge = ept.Critical ? 1500m : 0m;
                    return consult + criticalSurcharge;
                };
            }

            // OutPatient
            return (patient) => 300m; // flat consultation fee
        }
    }
}