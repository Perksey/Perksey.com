#!/usr/bin/env python3
import json
import typing
import requests

questions = []
final_questions = {
  "numQuestions": 40,
  "passMark": 26,
  "numMinutes": 60,
  "categories": {
    "IT Service Management": 40
  },
  "name": "Axelos ITIL 4 Foundation Practice Test"
}
final_questions["questions"] = {x: [] for x in final_questions["categories"].keys()}
n_exams = 20

URL_FMT = "https://raw.githubusercontent.com/Ditectrev/ITIL-4-Foundation-IT-Service-Management-Practice-Tests-Exams-Questions-Answers/main/README.md"

question = {}
req = requests.get(URL_FMT)
req.raise_for_status()
for line in req.text.splitlines():
    if not isinstance(line, str):
        continue
    line = line.strip()
    if line.startswith("### "):
        line = line[4:]
        if "answers" not in question or len(question["answers"]) != 4:
            print("Discarding WIP question: " + json.dumps(question))
        else:
            questions.append(question)
        question = {}
        question["text"] = line
    elif line.startswith("- ["):
        if line[4] != "]":
            continue
        if "answers" not in question:
            question["answers"] = []
        question["answers"].append({"text": line[5:].strip(), "correct": line[3].lower() == "x"})
if "answers" not in question or len(question["answers"]) != 4:
    print("Discarding WIP question: " + json.dumps(question))
else:
    questions.append(question)

final_questions["questions"]["IT Service Management"] = questions
for qs in final_questions["questions"].values():
    start = 0
    while start != -1:
        start = -1
        for i, q in enumerate(qs, start):
            n_correct = sum(1 for x in q["answers"] if x["correct"])
            if n_correct != 1:
                print(f"Discarding question with incorrect number of correct answers ({n_correct}): {json.dumps(q)}")
                qs.remove(q)
                start = i
                break

with open("itil4fdn.json", "w") as f:
    f.write(json.dumps(final_questions, indent=2))
