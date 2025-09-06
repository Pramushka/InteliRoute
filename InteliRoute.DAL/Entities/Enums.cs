using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InteliRoute.DAL.Entities;

public enum Intent { Support = 0, Sales = 1, Billing = 2, Careers = 3, IT = 4, Spam = 5, Other = 6 }
public enum RouteStatus { New = 0, Routed = 1, Triage = 2, Failed = 3 }
public enum ActionType { RouteToDepartment = 0, RouteToEmail = 1, SendToTriage = 2, Ignore = 3 }
public enum OutcomeType { Applied = 0, Skipped = 1, Failed = 2 }
public enum FetchMode { Polling = 0, Push = 1 }

