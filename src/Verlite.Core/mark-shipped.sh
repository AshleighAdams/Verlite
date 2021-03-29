#!/bin/bash
set -eou pipefail

cd "$(dirname "$0")"

shipped="$(cat PublicAPI.Shipped.txt PublicAPI.Unshipped.txt \
	| sort --ignore-case \
	| uniq)"

echo "$shipped" > PublicAPI.Shipped.txt
echo "#nullable enable" > PublicAPI.Unshipped.txt
