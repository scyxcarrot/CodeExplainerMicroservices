@echo off
rem This script copies all files with a .cs extension from a source directory
rem to a destination directory, preserving the directory structure.
rem It is designed to be run in the Windows Command Prompt.

rem --- Configuration ---
rem Set the source directory (where the files will be copied FROM)
rem Please replace 'C:\path\to\source\folder' with your actual path.
set "SOURCE_DIR=C:\Users\jwong\Desktop\IDS_GIT\IDS"

rem Set the destination directory (where the files will be copied TO)
rem Please replace 'C:\path\to\destination\folder' with your actual path.
set "DEST_DIR=C:\Users\jwong\Desktop\tutorial\CodeExplainer\IDSCodeExplainer\Data2"

rem --- Script Logic ---

rem Check if the source directory exists
if not exist "%SOURCE_DIR%" (
echo Error: Source directory '%SOURCE_DIR%' not found.
goto :end
)

rem Robocopy is a robust command-line tool for file copying in Windows.
rem It is often preferred over xcopy for its advanced features.
rem /S : Copies subdirectories, but not empty ones.
rem /E : Copies subdirectories, including empty ones.
rem /V : Produces verbose output, showing skipped files.
rem We specify *.cs to only copy files with that extension.
echo Copying C# files from '%SOURCE_DIR%' to '%DEST_DIR%'...
robocopy "%SOURCE_DIR%" "%DEST_DIR%" *.cs /S /V

rem Check the Robocopy exit code to see if the copy was successful
rem 0 = No files copied. No errors. No failures.
rem 1 = All files copied successfully.
if %errorlevel% leq 1 (
echo Successfully copied all .cs files.
) else (
echo An error occurred during the copy process.
)

:end
pause