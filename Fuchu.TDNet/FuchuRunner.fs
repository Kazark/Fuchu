﻿namespace Fuchu.TDNet

open TestDriven.Framework
open Fuchu
open Fuchu.Impl

type FuchuRunner() =
    let locker = obj()
    let funLock x = lock locker x
    let onPassed (listener: ITestListener) name time = 
        funLock <| fun _ -> 
            let result = TestResult(Name = name, 
                                    State = TestState.Passed,
                                    TimeSpan = time)
            listener.TestFinished result

    let onIgnored (listener: ITestListener) name reason = 
        funLock <| fun _ ->
            let result = TestResult(Name = name,
                                    State = TestState.Ignored,
                                    Message = reason)
            listener.TestFinished result

    let onFailed (listener: ITestListener) name error time = 
        funLock <| fun _ -> 
            let result = TestResult(Name = name, 
                                    State = TestState.Failed, 
                                    Message = error,
                                    TimeSpan = time)
            listener.TestFinished result

    let onException (listener: ITestListener) name ex time =
        funLock <| fun _ -> 
            let result = TestResult(Name = name, 
                                    State = TestState.Failed, 
                                    Message = ex.ToString(), 
                                    StackTrace = ex.ToString(),
                                    TimeSpan = time)
            listener.TestFinished result

    let run listener = 
        let printers = 
            { TestPrinters.BeforeRun = ignore
              Passed = onPassed listener
              Ignored = onIgnored listener
              Failed = onFailed listener
              Exception = onException listener }
        eval printers pmap

    let resultCountsToTDNetResult (c: TestResultCounts) =
        match c.Passed, c.Failed, c.Errored with
        | _, _, x when x > 0 -> TestRunState.Error
        | _, x, _ when x > 0 -> TestRunState.Failure
        | 0, 0, 0            -> TestRunState.NoTests
        | _                  -> TestRunState.Success

    let runAndSummary listener =
        function
        | Some test -> run listener test |> sumTestResults |> resultCountsToTDNetResult
        | None -> TestRunState.NoTests

    interface ITestRunner with
        member x.RunAssembly(listener, assembly) = 
            testFromAssembly assembly |> runAndSummary listener
        member x.RunMember(listener, assembly, metod) = 
            testFromMember metod |> runAndSummary listener 
        member x.RunNamespace(listener, assembly, ns) =
            testFromAssemblyWithFilter (fun t -> t.Namespace = ns) assembly
            |> runAndSummary listener 