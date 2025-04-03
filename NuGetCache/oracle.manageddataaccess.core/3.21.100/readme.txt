Oracle.ManagedDataAccess.Core NuGet Package 3.21.100 README
===========================================================
Release Notes: Oracle Data Provider for .NET Core

March 2023

This provider supports .NET 6 & .NET 7.

This README supplements the main ODP.NET 21c documentation.
https://docs.oracle.com/en/database/oracle/oracle-database/21/odpnt/


Bug Fixes since Oracle.ManagedDataAccess.Core NuGet Package 3.21.90
===================================================================
Bug 35155436 MAPPING FROM .NET TIMEZONE TO ORACLE TIMEZONE IS NOT TERRITORY/REGION BASED
Bug 35100428 CONNECTION CREATION FAILURES CAUSE THREADS AND POOLMANAGER OBJECTS TO ACCUMULATE
Bug 34873260 MORE SESSIONS CREATED THAN NECESSARY WHEN USING PROXY CONNECTIONS
Bug 32812583 NULLREFERENCEEXCEPTION WHEN HAVING ADDRESS_LIST WITHIN ADDRESS_LIST


Known Issues and Limitations
============================
1) BindToDirectory throws NullReferenceException on Linux when LdapConnection AuthType is Anonymous

https://github.com/dotnet/runtime/issues/61683

This issue is observed when using System.DirectoryServices.Protocols, version 6.0.0.
To workaround the issue, use System.DirectoryServices.Protocols, version 5.0.1.

 Copyright (c) 2021, 2023, Oracle and/or its affiliates. 
