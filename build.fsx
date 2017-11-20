#r @"packages/build/FAKE/tools/FakeLib.dll"
open Fake
open Fake.Git
open Fake.ReleaseNotesHelper

let outDir = "bin"

let gitOwner = "persimmon-projects"

let gitName = "Persimmon.Templates"

let release = LoadReleaseNotes "RELEASE_NOTES.md"

Target "Clean" (fun _ ->
  CleanDirs ["bin"; "temp"]
  !! "./src/**/bin"
  |> CleanDirs
  !! "./src/**/obj"
  |> CleanDirs
)

Target "NuGet" (fun _ ->

  NuGetPack (fun p ->
    {
      p with
        OutputPath = outDir
        WorkingDir = outDir
        Version = release.NugetVersion
        ReleaseNotes = toLines release.Notes
    }
  ) "./Persimmon.Templates.nuspec"
)

Target "PublishNuget" (fun _ ->
  Paket.Push(fun p ->
    { p with
        WorkingDir = outDir })
)

#load "paket-files/build/fsharp/FAKE/modules/Octokit/Octokit.fsx"
open Octokit

Target "Release" (fun _ ->
    StageAll ""
    Git.Commit.Commit "" (sprintf "Bump version to %s" release.NugetVersion)
    Branches.push ""

    Branches.tag "" release.NugetVersion
    Branches.pushTag "" "origin" release.NugetVersion

    // release on github
    createClient (getBuildParamOrDefault "github-user" "") (getBuildParamOrDefault "github-pw" "")
    |> createDraft gitOwner gitName release.NugetVersion (release.SemVer.PreRelease <> None) release.Notes
    // TODO: |> uploadFile "PATH_TO_FILE"
    |> releaseDraft
    |> Async.RunSynchronously
)

Target "All" DoNothing

"Clean"
  ==> "All"

"All"
  ==> "NuGet"

"NuGet"
  ==> "PublishNuget"
  ==> "Release"

RunTargetOrDefault "All"
