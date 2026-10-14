"""Synthetic classifier/provenance checks, never runtime evidence."""
import copy
import json
from pathlib import Path
import tempfile
import unittest
import xml.etree.ElementTree as ET

import verify

from unittest.mock import patch
import zipfile

def evidence(mode):
    n = verify.NS['t']
    tag = lambda x: '{' + n + '}' + x
    root = ET.Element(tag('TestRun'))
    summary = ET.SubElement(root, tag('ResultSummary'))
    positive = mode == 'positive'
    ET.SubElement(summary, tag('Counters'), total='1', executed='1', passed='1' if positive else '0', failed='0' if positive else '1', completed='0')
    defs = ET.SubElement(root, tag('TestDefinitions'))
    d = ET.SubElement(defs, tag('UnitTest'), id='one')
    ET.SubElement(d, tag('TestMethod'), className=verify.CLASS, name=verify.TEST)
    results = ET.SubElement(root, tag('Results'))
    r = ET.SubElement(results, tag('UnitTestResult'), testId='one', testName=verify.TEST, outcome='Passed' if positive else 'Failed')
    output = ET.SubElement(r, tag('Output'))
    arrays = 3000 if mode == 'baseline' else 1
    ET.SubElement(output, tag('StdOut')).text = f'RESOURCE_BYTES_OBSERVATION: reads=3000 arrays={arrays} bytes={arrays * 65536} ids=3000\nRESOURCE_BYTES_COLLISION: ' + {'positive': 'checked', 'baseline': 'historical', 'hash-only': 'mismatch'}[mode] + '\n' + verify.COMPLETE + '\n'
    if not positive:
        error = ET.SubElement(output, tag('ErrorInfo'))
        message = 'RESOURCE_BYTES_SHARED\nAssert.That(arrays, Is.EqualTo(1))\nExpected: 1\nBut was: 3000' if mode == 'baseline' else 'RESOURCE_BYTES_COLLISION_SAFE\nAssert.That(collisionCorrect, Is.True)\nExpected: True\nBut was: False'
        ET.SubElement(error, tag('Message')).text = message
        ET.SubElement(error, tag('StackTrace')).text = 'at ' + verify.CLASS + '.CheckResources(DreamManager manager, DreamObjectTree tree, DreamResourceManager resources, DreamRefManager refs, Byte[] payload) in /checkout/Content.IntegrationTests/SavefileResourceBytesTests.cs:line 1'
    log = ('Passed' if positive else 'Failed') + ' ' + verify.TEST + '\nTest Run ' + ('Successful.' if positive else 'Failed.') + '\n'
    return (root, log, 0 if positive else 1)

class ParserTests(unittest.TestCase):

    def run_case(self, mode, change=None):
        root, log, code = evidence(mode)
        if change:
            root, log, code = change(root, log, code)
        return verify.verify(ET.tostring(root), log, mode, code)

    def test_three_intended_cases(self):
        for mode in ('positive','baseline','hash-only'):
            self.assertTrue(self.run_case(mode))

    def test_observed_nunit_boundary_whitespace(self):

        def mutate(r, l, c):
            message = r.find('.//t:Message', verify.NS)
            message.text = '  ' + message.text + '\n'
            return (r, l, c)
        for mode in ('baseline', 'hash-only'):
            self.assertTrue(self.run_case(mode, mutate))

    def test_reject_unknown_message_prefix(self):

        def mutate(r, l, c):
            message = r.find('.//t:Message', verify.NS)
            message.text = 'UNKNOWN_FAILURE\n  ' + message.text
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('baseline', mutate)

    def test_zero_not_executed_counter(self):

        def mutate(r, l, c):
            r.find('.//t:Counters', verify.NS).set('notExecuted', '0')
            return (r, l, c)
        self.assertTrue(self.run_case('positive', mutate))

    def test_reject_nonzero_not_executed(self):

        def mutate(r, l, c):
            r.find('.//t:Counters', verify.NS).set('notExecuted', '1')
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('positive', mutate)

    def test_known_async_frames_and_numbered_stack(self):

        def mutate(r, l, c):
            stack = r.find('.//t:StackTrace', verify.NS)
            stack.text += '\n--- End of stack trace from previous location ---\nat NUnit.Framework.Internal.TaskAwaitAdapter.GenericAdapter`1.BlockUntilCompleted()\n1) ' + stack.text
            return (r, l, c)
        self.assertTrue(self.run_case('baseline', mutate))

    def test_reject_wrong_numbered_stack(self):

        def mutate(r, l, c):
            stack = r.find('.//t:StackTrace', verify.NS)
            stack.text += '\n2) ' + stack.text
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('baseline', mutate)

    def test_reject_unknown_async_frame(self):

        def mutate(r, l, c):
            stack = r.find('.//t:StackTrace', verify.NS)
            stack.text += '\nat NUnit.Framework.Internal.Arbitrary.Run()'
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('baseline', mutate)

    def test_reject_runtime_error(self):
        with self.assertRaises(AssertionError):
            self.run_case('positive', lambda r, l, c: (r, l + '[ERRO] Bad ref', c))

    def test_reject_compile_error(self):
        with self.assertRaises(AssertionError):
            self.run_case('baseline', lambda r, l, c: (r, l + ' error CS1000', c))

    def test_reject_arbitrary_build_failure(self):
        with self.assertRaises(AssertionError):
            self.run_case('baseline', lambda r, l, c: (r, l + 'Build FAILED.\n1 Warning(s)\n1 Error(s)\nTime Elapsed 00:00:01', c))

    def test_exact_vstest_zero_error_footer(self):
        self.assertTrue(self.run_case('baseline', lambda r, l, c: (r, l + 'Build FAILED.\n0 Warning(s)\n0 Error(s)\nTime Elapsed 00:00:01', c)))

    def test_reject_duplicate_marker(self):

        def mutate(r, l, c):
            out = r.find('.//t:StdOut', verify.NS)
            out.text += '\n' + verify.COMPLETE
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('positive', mutate)

    def test_reject_extra_test(self):

        def mutate(r, l, c):
            r.find('t:Results', verify.NS).append(copy.deepcopy(r.find('.//t:UnitTestResult', verify.NS)))
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('positive', mutate)

    def test_reject_counter_change(self):

        def mutate(r, l, c):
            r.find('.//t:Counters', verify.NS).set('executed', '0')
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('positive', mutate)

    def test_reject_foreign_frame(self):

        def mutate(r, l, c):
            r.find('.//t:StackTrace', verify.NS).text += '\nat Foreign.Work()'
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('baseline', mutate)

    def test_reject_changed_observation(self):

        def mutate(r, l, c):
            out = r.find('.//t:StdOut', verify.NS)
            out.text = out.text.replace('ids=3000', 'ids=2999')
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('positive', mutate)

    def test_reject_wrong_exit(self):
        with self.assertRaises(AssertionError):
            self.run_case('baseline', lambda r, l, c: (r, l, 0))

    def test_reject_unknown_assertion(self):

        def mutate(r, l, c):
            out = r.find('.//t:Message', verify.NS)
            out.text += '\nOTHER_FAILURE'
            return (r, l, c)
        with self.assertRaises(AssertionError):
            self.run_case('hash-only', mutate)
