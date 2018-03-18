@echo off
setlocal

rem enumerate each folder under root and check for existence of project.json
rem if project.json exists, touch to trigger NuGet restore
for /d %%d in (..\wwwroot\*) do (  
  echo check %%d
  pushd %%d
  if exist project.json (
    echo touching project.json to trigger NuGet
    call touch project.json
  ) else (
    echo no project.json found
  )
  popd 
)