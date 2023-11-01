import json
import re
import os
import glob
from typing import List

input_folders = [
    "..\\Editor/",
    "..\\Runtime/",
]
output_folder = "docs/reference"


class ClassDesc:
    className: str
    summary: str


class FileObj:
    path: List[str]
    fileName: str
    classes: List[ClassDesc]


output_json: List[FileObj] = []

for folder in input_folders:
    for file in glob.iglob(folder + "**/*.cs", recursive=True):
        fileObj = {
            "path": [
                f for f in file.split("\\") if f != ".." and not f.endswith(".cs")
            ],
            "fileName": os.path.basename(file),
        }

        with open(file, "r", encoding="utf8") as f:
            content = f.read()
            print(file)
            matches = re.findall(
                "(?:<summary>\n((?:[^;])*)\/\/\/\s?<\/summary>[^;]*)?public class (\S*)\s",
                content,
            )
            fileObj["classes"] = []
            for m in matches:
                fileObj["classes"].append(
                    {
                        "className": m[1],
                        "summary": m[0].replace("///", "").strip(),
                    }
                )

        print(fileObj)
        output_json.append(fileObj)

with open("docs_structure.json", "w") as f:
    json.dump(output_json, f, indent=4)

for fileObj in output_json:
    new_path = os.path.join(output_folder, os.path.join(*fileObj["path"]))
    os.makedirs(new_path, exist_ok=True)
    with open(
        os.path.join(new_path, fileObj["fileName"].replace(".cs", ".md")),
        "w",
        encoding="utf8",
    ) as f:
        f.write("# " + fileObj["fileName"] + "\n\n")
        for c in fileObj["classes"]:
            f.write("## " + c["className"] + "\n\n" + c["summary"] + "\n\n")
