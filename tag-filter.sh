#!/bin/bash

echo git tag -n999 --format='%(contents:body)' "$VERLITE_TAG"
body="$(git tag -n999 --format='%(contents:body)' "$VERLITE_TAG")"
echo body: $body
if [[ "$body" == *"#auto-tag"* ]]; then
	echo tag contains keyword
	exit 1
else
	echo tag does not contain keyword
	exit 0
fi