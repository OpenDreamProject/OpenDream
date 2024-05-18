import sys
import re

if __name__ == "__main__":
    log_files = sys.argv[1:]
    seen = set()
    lines = []

    warning_pattern = re.compile(r'##\[warning\].*') # catch warnings might want to catch errors?

    for log_file in log_files:
        with open(log_file, 'w+') as log:
            for line in log:
                if warning_pattern.search(line):
                    if line not in seen:
                        seen.add(line)
                        log.write(line)
                else:
                    log.write(line)
