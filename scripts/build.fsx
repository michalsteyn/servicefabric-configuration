#I "packages/FAKE.Core/tools/"
#r "FakeLib.dll"
#I "packages/FAKE.Dotnet/tools/"
#r "Fake.Dotnet.dll"

open Fake
open Fake.Git
open Fake.Dnx
open Fake.SemVerHelper

let mutable version : SemVerHelper.SemVerInfo option = None

Target "Clean" (fun _ ->
    !! "artifacts" ++ "src/*/bin" ++ "test/*/bin"
        |> DeleteDirs
)

Target "UpdateVersion" (fun _ ->   
    // compute commit count
    let repositoryDir = currentDirectory
    let currentSha = getCurrentSHA1 repositoryDir

    // get current version prefix from semver file
    let versionPrefix = System.IO.File.ReadLines "semver" |> Seq.tryHead
    if versionPrefix.IsNone then
        failwith "Unable to read semver prefix"

    // parse semver prefix
    tracefn "Semver file prefix is %s" versionPrefix.Value
    let semver = parse versionPrefix.Value    

    let semverCreatedSha = runSimpleGitCommand repositoryDir <| sprintf "log -G\"%s\" --reverse --max-count=1 --format=format:%%H -- semver" versionPrefix.Value
    let comitSinceCreated = runSimpleGitCommand repositoryDir <| sprintf "rev-list --no-merges --count %s..%s" semverCreatedSha currentSha

    let prereleaseInfo = 
        match semver.PreRelease with
        | Some ver ->     
            let buildCounterFixed = comitSinceCreated.PadLeft(3, '0')             
            let versionWithBuild = sprintf "%s-%s" ver.Origin buildCounterFixed           
            tracefn "Prerelease version: %A" versionWithBuild
            Some {
                PreRelease.Origin = versionWithBuild
                Name = versionWithBuild
                Number = None
            }
        | _ -> None

    version <- Some { semver with PreRelease = prereleaseInfo }    
)

Target "UpgradeDnx" (fun _ ->
    DnvmUpgrade id    
)

Target "BuildProjects" (fun _ ->
    let sdkVersion = GlobalJsonSdk "global.json"
    tracefn "Using global.json sdk version: %s" sdkVersion

    //set sdk version from global.json
    let setRuntimeOptions options = 
        { options with 
            VersionOrAlias = sdkVersion 
        }

    // set version suffix for project.json version template (1.0.0-*)
    if version.IsSome && version.Value.PreRelease.IsSome then       
        let prerelease = version.Value.PreRelease.Value.Origin
        tracefn "Prerelese part: %s" prerelease
        SetDnxVersionSuffix prerelease

    !! "src/*/project.json" 
        |> Seq.iter(fun proj ->  

            // restore packages
            DnuRestore (fun o -> 
                { o with 
                    Runtime = o.Runtime |> setRuntimeOptions
                }) proj

            // build (pack) project and generate outputs
            DnuPack (fun o -> 
                { o with 
                    Runtime = o.Runtime |> setRuntimeOptions
                    Configuration = Release
                    OutputPath = Some (currentDirectory @@ "artifacts")
                }) proj
        )
)

Target "Default" <| DoNothing

"Clean"
    ==> "UpgradeDnx"
    ==> "UpdateVersion"
    ==> "BuildProjects"
    ==> "Default"

// start build
RunTargetOrDefault "Default"