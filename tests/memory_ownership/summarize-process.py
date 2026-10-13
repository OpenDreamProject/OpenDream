"""Summarize passive benchmark exports without treating allocated bytes as retained."""
import argparse
import csv
import json
from pathlib import Path
import statistics


def read_csv(path):
    with path.open(encoding='utf-8-sig', newline='') as stream:
        return list(csv.DictReader(stream))


def summarize(directory):
    process = read_csv(directory / 'process.csv')
    topic = read_csv(directory / 'topic.csv')
    counters = read_csv(directory / 'counters.csv')
    groups = {}
    for row in counters:
        groups.setdefault(row['Timestamp'], {})[row['Counter Name']] = float(row['Mean/Increment'])
    snapshots = list(groups.values())
    first, last = process[0], process[-1]
    seconds = float(last['elapsed_seconds']) - float(first['elapsed_seconds'])
    def heap(snapshot):
        total = sum(v for k, v in snapshot.items() if k.startswith('dotnet.gc.last_collection.heap.size '))
        fragment = sum(v for k, v in snapshot.items() if k.startswith('dotnet.gc.last_collection.heap.fragmentation.size '))
        return total - fragment
    def interval_total(prefix):
        return sum(v for sample in snapshots for k, v in sample.items() if k.startswith(prefix))
    latency = sorted(float(row['request_ms']) for row in topic)
    game_seconds = ((float(topic[-1]['round_deciseconds']) - float(topic[0]['round_deciseconds'])) / 10) if len(topic) > 1 else None
    topic_seconds = (float(topic[-1]['elapsed_seconds']) - float(topic[0]['elapsed_seconds'])) if len(topic) > 1 else None
    fragment = (directory / 'server-window.log').read_text(errors='replace')
    return {
        'directory': str(directory), 'receipt': json.loads((directory / 'receipt.json').read_text(encoding='utf-8-sig')),
        'elapsed_seconds': seconds, 'players_observed': sorted({int(row['players']) for row in topic}),
        'game_ids': sorted({row['game_id'] for row in topic}),
        'private_bytes_start': int(first['private_bytes']), 'private_bytes_end': int(last['private_bytes']),
        'private_bytes_peak': max(int(row['private_bytes']) for row in process),
        'working_bytes_start': int(first['working_bytes']), 'working_bytes_end': int(last['working_bytes']),
        'cpu_cores_mean': (float(last['cpu_seconds']) - float(first['cpu_seconds'])) / seconds,
        'topic_request_ms_median': statistics.median(latency) if latency else None,
        'topic_request_ms_max': max(latency) if latency else None,
        'game_clock_seconds_per_real_second': game_seconds / topic_seconds if topic_seconds else None,
        'allocated_bytes_interval_sum': interval_total('dotnet.gc.heap.total_allocated '),
        'allocated_bytes_per_second_approximate': interval_total('dotnet.gc.heap.total_allocated ') / max(len(snapshots)-1, 1),
        'gc_collections_by_generation': {generation: sum(v for sample in snapshots for k, v in sample.items() if k.startswith('dotnet.gc.collections ') and f'gc.heap.generation={generation}' in k) for generation in ['gen0', 'gen1', 'gen2']},
        'last_gc_occupied_heap_bytes_start': heap(snapshots[0]),
        'last_gc_occupied_heap_bytes_end': heap(snapshots[-1]),
        'mainloop_cannot_keep_up_warnings': fragment.count('MainLoop: Cannot keep up!'),
        'error_log_lines': [line for line in fragment.splitlines() if '[ERRO]' in line or '[ERROR]' in line],
        'limits': 'Last-GC occupied heap is an estimate that can be stale and includes promoted live objects. Short windows, different process ages and unequal player workloads cannot establish long-run leak elimination or gameplay acceptance.'
    }


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('directories', nargs='+', type=Path)
    parser.add_argument('--output', required=True, type=Path)
    args = parser.parse_args()
    result = [summarize(directory) for directory in args.directories]
    args.output.write_text(json.dumps(result, indent=2), encoding='utf-8')
    print(json.dumps(result, indent=2))
